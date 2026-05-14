using System;
using System.Linq;
using Npgsql;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Publication.Models;
using Pollon.Content.Api;
using Pollon.Content.Api.Services;
using Pollon.Content.Api.Domain.Interfaces;
using Pollon.Content.Api.Infrastructure.Repositories;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add PostgreSQL DataSource and DbContext
builder.AddNpgsqlDataSource("contentdb");

builder.AddNpgsqlDbContext<ApiDbContext>("contentdb");

// Setup Keycloak Token Service (for service-to-service auth)
builder.Services.AddHttpClient<KeycloakTokenService>();

// Setup Backoffice API Client
builder.Services.AddHttpClient<BackofficeApiClient>(client =>
{
    client.BaseAddress = new("https+http://backofficeapi");
});

builder.Services.AddSingleton<ITemplateRenderer, ScribanTemplateRenderer>();

// MinIO Client and Static Storage
builder.AddMinioClient("minio");
builder.Services.AddSingleton<IStaticStorage, MinioStaticStorage>();

// Register Repositories
builder.Services.AddScoped<IPublishedContentRepository, PublishedContentRepository>();
builder.Services.AddScoped<IContentSubmissionRepository, ContentSubmissionRepository>();



// Setup Wolverine and configure RabbitMQ
builder.Host.UseWolverine(opts =>
{
    var connectionString = builder.Configuration.GetConnectionString("contentdb")!;

    opts.PersistMessagesWithPostgresql(connectionString);

    opts.UseRabbitMq(builder.Configuration.GetConnectionString("messaging")!)
        .AutoProvision()
        .UseConventionalRouting(convention =>
        {
            convention.QueueNameForListener(type => $"content-api-{type.Name.ToLower()}");
        });

    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.UseDurableInboxOnAllListeners();
    opts.Policies.UseDurableLocalQueues();
});

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Setup Authentication
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakUrl = builder.Configuration.GetConnectionString("keycloak");
        if (string.IsNullOrEmpty(keycloakUrl))
        {
            keycloakUrl = builder.Configuration["services:keycloak:http:0"] ?? "http://localhost:8080";
        }
        if (!keycloakUrl.StartsWith("http")) keycloakUrl = $"http://{keycloakUrl}";

        options.Authority = $"{keycloakUrl.TrimEnd('/')}/realms/Pollon";
        options.Audience = "content"; // Or whatever audience is appropriate for public API
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            ClockSkew = TimeSpan.FromMinutes(5),
            SignatureValidator = delegate (string token, Microsoft.IdentityModel.Tokens.TokenValidationParameters parameters)
            {
                var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
                return jwt;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Ensure the database is created and Storage is initialized
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

    // In development, ensure the database is created
    await dbContext.Database.EnsureCreatedAsync();

    var storage = scope.ServiceProvider.GetRequiredService<IStaticStorage>();
    await storage.InitializeAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


var contentApi = app.MapGroup("/api/content").WithTags("Published Content");

contentApi.MapGet("/", async ([AsParameters] ContentQueryParameters query, IPublishedContentRepository repository) =>
{
    var (items, totalCount) = await repository.GetPaginatedAsync(query);
    return Results.Ok(new PagedResult<PublishedContent>(items, totalCount, query.Page, query.PageSize));
});

// Recupera il singolo contenuto per ID
contentApi.MapGet("/item/{id}", async (string id, IPublishedContentRepository repository) =>
{
    var item = await repository.GetByIdAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
});

// Recupera i contenuti per Slug (supporta percorsi annidati con {*slug})
contentApi.MapGet("/{*slug}", async (string slug, [AsParameters] ContentQueryParameters query, IPublishedContentRepository repository) =>
{
    var (items, totalCount) = await repository.GetBySlugPaginatedAsync(slug, query);
    return Results.Ok(new PagedResult<PublishedContent>(items, totalCount, query.Page, query.PageSize));
});

// Interactive Content Submission Endpoint
contentApi.MapPost("/submit/{id}", async (string id, [Microsoft.AspNetCore.Mvc.FromBody] JsonElement payload, IPublishedContentRepository contentRepo, IContentSubmissionRepository submissionRepo, IMessageBus messageBus, ClaimsPrincipal user) =>
{
    var item = await contentRepo.GetByIdAsync(id) ?? throw new BadHttpRequestException("Content not found.");

    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var userName = user.FindFirst("preferred_username")?.Value;

    var submission = new ContentSubmission
    {
        Id = Guid.NewGuid().ToString(),
        ContentItemId = item.Id,
        SystemName = item.SystemName,
        UserId = userId,
        UserName = userName,
        SubmittedAt = DateTime.UtcNow,
        JsonData = payload.GetRawText()
    };

    await submissionRepo.AddAsync(submission);

    // Publish event
    var evt = new Pollon.Contracts.Events.ContentSubmittedEvent(
        submission.Id,
        submission.ContentItemId,
        submission.SystemName,
        submission.UserId,
        submission.UserName,
        submission.JsonData
    );
    await messageBus.PublishAsync(evt);

    return Results.Ok(new { Message = "Submission successful", SubmissionId = submission.Id });
}).RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();


