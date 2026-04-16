using Pollon.Backoffice.Repositories;
using Pollon.Backoffice.Services;

namespace Pollon.Backoffice.Mcp.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddBackofficeServices(this IServiceCollection services)
    {
        // Register repositories and domain services
        services.AddScoped(typeof(IRepository<>), typeof(MartenRepository<>));
        services.AddScoped<IContentItemService, ContentItemService>();

        return services;
    }
}
