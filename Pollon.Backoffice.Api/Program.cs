using Wolverine;
using Wolverine.RabbitMQ;
using Marten;
using Pollon.Backoffice.Models;
using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;
using Pollon.Backoffice.Api.Services;
using Pollon.Backoffice.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddBackofficeAuthentication(builder.Configuration);

// Configure JSON to ignore cycles for hierarchical data
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Setup Marten
builder.Services.AddMarten((Marten.StoreOptions opts) => 
{
    opts.Connection(builder.Configuration.GetConnectionString("backofficedb")!);
    opts.Schema.For<ContentItem>().NgramIndex(x => x.SearchText);
    
    // Align Marten with System.Text.Json enum settings used by the API
    opts.Serializer(new Marten.Services.SystemTextJsonSerializer
    {
        EnumStorage = Weasel.Core.EnumStorage.AsString
    });
})
.UseLightweightSessions();

builder.Services.AddScoped(typeof(IRepository<>), typeof(MartenRepository<>));

builder.Services.AddScoped<IContentItemService, ContentItemService>();

builder.Services.AddHttpClient("MediaApi", client =>
{
    client.BaseAddress = new("https+http://mediaapi");
});

// Setup Wolverine / RabbitMQ
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq(builder.Configuration.GetConnectionString("messaging")!)
        .AutoProvision()
        .UseConventionalRouting();

    opts.Policies.UseDurableLocalQueues();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map Endpoints
app.MapContentTypeEndpoints();
app.MapContentItemEndpoints();
app.MapGalleryEndpoints();
app.MapMediaEndpoints();

app.Run();
