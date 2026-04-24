using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;

namespace Pollon.Backoffice.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddBackofficeServices(this IServiceCollection services)
    {
        // Configure JSON options
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

        // Register repositories and domain services
        services.AddScoped(typeof(IRepository<>), typeof(MartenRepository<>));
        services.AddScoped<IContentItemService, ContentItemService>();
        services.AddScoped<IKeycloakAdminClient, KeycloakAdminClient>();
        services.AddHostedService<PluginSyncService>();

        return services;
    }
}

