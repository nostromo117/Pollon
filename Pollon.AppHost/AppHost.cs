using CommunityToolkit.Aspire.Hosting.Minio;

var builder = DistributedApplication.CreateBuilder(args);

var postgresServer = builder.AddPostgres("postgres")
    .WithDataVolume();
var postgres = postgresServer.AddDatabase("backofficedb");
var contentDb = postgresServer.AddDatabase("contentdb");
var keycloak = builder.AddKeycloak("keycloak")
    .WithDataBindMount("./keycloak-data")
    .WithBindMount("./keycloak-config", "/opt/keycloak/data/import", isReadOnly: true)
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin");

var messaging = builder.AddRabbitMQ("messaging");
var minio = builder.AddMinioContainer("minio")
    .WithDataVolume();

var consul = builder.AddContainer("consul", "hashicorp/consul", "latest")
    .WithHttpEndpoint(port: 8500, targetPort: 8500, name: "ui")
    .WithArgs("agent", "-dev", "-client", "0.0.0.0");

// Jaeger Tracing Backend (Open Source)
var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one")
    .WithHttpEndpoint(port: 16686, targetPort: 16686, name: "ui")
    .WithEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true");

// Community Toolkit OTel Collector
var otelCollector = builder.AddOpenTelemetryCollector("otel-collector")
    .WithBindMount("./config/otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml", isReadOnly: true)
    .WithArgs("--config", "/etc/otelcol-contrib/config.yaml");

var mediaApi = builder.AddProject<Projects.Pollon_Media_Api>("mediaapi")
    .WithReference(postgres)
    .WithReference(keycloak)
    .WithEnvironment("JAEGER_OTLP_ENDPOINT", otelCollector.GetEndpoint("grpc"))
    .WithEnvironment("JAEGER_OTLP_PROTOCOL", "grpc")
    .WaitFor(postgresServer)
    .WaitFor(keycloak)
    .WaitFor(otelCollector);

var backofficeApi = builder.AddProject<Projects.Pollon_Backoffice_Api>("backofficeapi")
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(messaging)
    .WithReference(keycloak)
    .WithReference(mediaApi)
    .WithEnvironment("JAEGER_OTLP_ENDPOINT", otelCollector.GetEndpoint("grpc"))
    .WithEnvironment("JAEGER_OTLP_PROTOCOL", "grpc")
    .WaitFor(postgresServer)
    .WaitFor(keycloak)
    .WaitFor(mediaApi)
    .WaitFor(otelCollector);

var contentApi = builder.AddProject<Projects.Pollon_Content_Api>("contentapi")
    .WithReference(contentDb)
    .WithReference(messaging)
    .WithReference(backofficeApi)
    .WithReference(keycloak)
    .WithReference(minio)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("JAEGER_OTLP_ENDPOINT", otelCollector.GetEndpoint("grpc"))
    .WithEnvironment("JAEGER_OTLP_PROTOCOL", "grpc")
    .WaitFor(postgresServer)
    .WaitFor(messaging)
    .WaitFor(keycloak)
    .WaitFor(otelCollector);

var backofficeWeb = builder.AddProject<Projects.Pollon_Backoffice_Web>("backoffice-web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(backofficeApi)
    .WithReference(mediaApi)
    .WithReference(keycloak)
    .WithEnvironment("JAEGER_OTLP_ENDPOINT", otelCollector.GetEndpoint("grpc"))
    .WithEnvironment("JAEGER_OTLP_PROTOCOL", "grpc")
    .WaitFor(backofficeApi)
    .WaitFor(mediaApi)
    .WaitFor(keycloak)
    .WaitFor(otelCollector);

builder.AddProject<Projects.Pollon_Backoffice_Mcp>("backoffice-mcp")
    .WithReference(postgres)
    .WithReference(messaging)
    .WaitFor(postgresServer);

var frontendWeb = builder.AddProject<Projects.Pollon_Frontend_Web>("frontend-web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(contentApi)
    .WithReference(backofficeApi)
    .WithReference(mediaApi)
    .WithReference(minio)
    .WithEnvironment("ReverseProxy__Clusters__minio-cluster__Destinations__minio-dest__Address", minio.GetEndpoint("http"))
    .WithEnvironment("JAEGER_OTLP_ENDPOINT", otelCollector.GetEndpoint("grpc"))
    .WithEnvironment("JAEGER_OTLP_PROTOCOL", "grpc")
    .WaitFor(contentApi)
    .WaitFor(otelCollector);

// --- PLUGIN SYSTEM ---

// Seeder: writes RabbitMQ connection string to Consul
builder.AddProject<Projects.Pollon_Configuration_Seeder>("config-seeder")
    .WithReference(messaging) 
    .WithEnvironment("CONSUL_URL", consul.GetEndpoint("ui"))
    .WaitFor(consul)
    .WaitFor(messaging);

builder.AddProject<Projects.Pollon_Plugin_Example>("plugin-example")
    .WithReference(messaging)
    .WithEnvironment("CONSUL_URL", consul.GetEndpoint("ui"))
    .WaitFor(consul)
    .WaitFor(messaging);

builder.Build().Run();

