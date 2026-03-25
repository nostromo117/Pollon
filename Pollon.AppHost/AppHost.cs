var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sqlserver");
var postgres = builder.AddPostgres("postgres").AddDatabase("backofficedb");
var keycloak = builder.AddKeycloak("keycloak", 8080);
var messaging = builder.AddRabbitMQ("messaging");

var mediaApi = builder.AddProject<Projects.Pollon_Media_Api>("mediaapi")
    .WithReference(postgres)
    .WaitFor(postgres);

var backofficeApi = builder.AddProject<Projects.Pollon_Backoffice_Api>("backofficeapi")
    .WithExternalHttpEndpoints()
    .WithReference(sql)
    .WithReference(postgres)
    .WithReference(messaging)
    .WaitFor(sql)
    .WaitFor(postgres);

var contentApi = builder.AddProject<Projects.Pollon_Content_Api>("contentapi")
    .WithReference(sql)
    .WithReference(messaging)
    .WithReference(backofficeApi)
    .WithHttpHealthCheck("/health")
    .WaitFor(sql)
    .WaitFor(messaging);

var backofficeWeb = builder.AddProject<Projects.Pollon_Backoffice_Web>("backoffice-web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(backofficeApi)
    .WithReference(mediaApi)
    .WaitFor(backofficeApi)
    .WaitFor(mediaApi);

var frontendWeb = builder.AddProject<Projects.Pollon_Frontend_Web>("frontend-web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(contentApi)
    .WithReference(backofficeApi)
    .WithReference(mediaApi)
    .WaitFor(contentApi);

builder.Build().Run();
