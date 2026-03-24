using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pollon.Content.Api.Data;
using Pollon.Contracts.Models;
using Pollon.Content.Api;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add SQL Server DbContext
builder.AddSqlServerDbContext<ApiDbContext>("sqlserver");

// Setup Backoffice API Client
builder.Services.AddHttpClient<BackofficeApiClient>(client =>
{
    client.BaseAddress = new("https+http://backofficeapi");
});



// Setup Wolverine and configure RabbitMQ
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq(builder.Configuration.GetConnectionString("messaging")!)
        .AutoProvision()
        .UseConventionalRouting();

    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.UseDurableInboxOnAllListeners();
    opts.Policies.UseDurableLocalQueues();
});

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Ensure the database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


var contentApi = app.MapGroup("/api/content").WithTags("Published Content");

contentApi.MapGet("/", async ([AsParameters] ContentQueryParameters query, ApiDbContext dbContext) =>
{
    var queryable = dbContext.PublishedContents.AsQueryable();
    var (items, totalCount) = await GetPaginatedResultsAsync(queryable, query);
    return Results.Ok(new PagedResult<PublishedContent>(items, totalCount, query.Page, query.PageSize));
});

// Recupera tutti i contenuti di un certo tipo (es: "blog-post") sfruttando l'indice su Slug
contentApi.MapGet("/{slug}", async (string slug, [AsParameters] ContentQueryParameters query, ApiDbContext dbContext) =>
{
    var queryable = dbContext.PublishedContents.Where(c => c.Slug == slug);
    var (items, totalCount) = await GetPaginatedResultsAsync(queryable, query);
    return Results.Ok(new PagedResult<PublishedContent>(items, totalCount, query.Page, query.PageSize));
});

// Recupera il singolo contenuto per ID
contentApi.MapGet("/item/{id}", async (string id, ApiDbContext dbContext) =>
{
    var item = await dbContext.PublishedContents.FindAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
});

app.MapDefaultEndpoints();

app.Run();

static async Task<(List<PublishedContent> Items, int TotalCount)> GetPaginatedResultsAsync(IQueryable<PublishedContent> queryable, ContentQueryParameters query)
{
    // Criteri di ricerca testuale (sul JSON per ora)
    if (!string.IsNullOrWhiteSpace(query.SearchTerm))
    {
        queryable = queryable.Where(c => c.JsonData.Contains(query.SearchTerm));
    }

    // Ordinamento
    if (query.SortBy?.Equals("PublishedAt", StringComparison.OrdinalIgnoreCase) == true)
    {
        queryable = query.SortDescending ? queryable.OrderByDescending(c => c.PublishedAt) : queryable.OrderBy(c => c.PublishedAt);
    }
    else
    {
        queryable = queryable.OrderBy(c => c.Id);
    }

    // Conteggio totale e Paginazione
    var totalCount = await EntityFrameworkQueryableExtensions.CountAsync(queryable);
    
    var itemsQuery = queryable
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize);
        
    var items = await EntityFrameworkQueryableExtensions.ToListAsync(itemsQuery);

    return (items, totalCount);
}

