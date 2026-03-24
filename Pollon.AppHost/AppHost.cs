var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sqlserver");
var postgres = builder.AddPostgres("postgres").AddDatabase("backofficedb");
var keycloak = builder.AddKeycloak("keycloak", 8080);
var messaging = builder.AddRabbitMQ("messaging");

var backofficeApi = builder.AddProject<Projects.Pollon_Backoffice_Api>("backofficeapi")
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
    .WaitFor(backofficeApi);

var frontendWeb = builder.AddProject<Projects.Pollon_Frontend_Web>("frontend-web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(contentApi)
    .WaitFor(contentApi);

builder.Build().Run();
