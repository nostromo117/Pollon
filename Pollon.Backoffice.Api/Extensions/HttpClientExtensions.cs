using Pollon.Backoffice.Services;

namespace Pollon.Backoffice.Api.Extensions;

public static class HttpClientExtensions
{
    public static IServiceCollection AddBackofficeHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("MediaApi", client =>
        {
            client.BaseAddress = new("https+http://mediaapi");
        });

        services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>();

        return services;
    }
}
