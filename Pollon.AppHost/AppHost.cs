using CommunityToolkit.Aspire.Hosting.Minio;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sqlserver")
    .WithDataVolume();
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("backofficedb");
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

var mediaApi = builder.AddProject<Projects.Pollon_Media_Api>("mediaapi")
    .WithReference(postgres)
    .WithReference(keycloak)
    .WaitFor(postgres)
    .WaitFor(keycloak);

var backofficeApi = builder.AddProject<Projects.Pollon_Backoffice_Api>("backofficeapi")
    .WithExternalHttpEndpoints()
    .WithReference(sql)
    .WithReference(postgres)
    .WithReference(messaging)
    .WithReference(keycloak)
    .WithReference(mediaApi)
    .WaitFor(sql)
    .WaitFor(postgres)
    .WaitFor(keycloak)
    .WaitFor(mediaApi);

var contentApi = builder.AddProject<Projects.Pollon_Content_Api>("contentapi")
    .WithReference(sql)
    .WithReference(messaging)
    .WithReference(backofficeApi)
    .WithReference(keycloak)
    .WithReference(minio)
    .WithHttpHealthCheck("/health")
    .WaitFor(sql)
    .WaitFor(messaging)
    .WaitFor(keycloak);

var backofficeWeb = builder.AddProject<Projects.Pollon_Backoffice_Web>("backoffice-web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(backofficeApi)
    .WithReference(mediaApi)
    .WithReference(keycloak)
    .WaitFor(backofficeApi)
    .WaitFor(mediaApi)
    .WaitFor(keycloak);

var frontendWeb = builder.AddProject<Projects.Pollon_Frontend_Web>("frontend-web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(contentApi)
    .WithReference(backofficeApi)
    .WithReference(mediaApi)
    .WaitFor(contentApi);

builder.Build().Run();
