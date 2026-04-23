using System;
using System.Linq;
using Npgsql;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pollon.Publication.Models;
using System.Reflection;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.Marten;
using Wolverine.Postgresql;

namespace Microsoft.AspNetCore.Builder;

public static class BackofficeInfrastructureExtensions
{
    public static IHostApplicationBuilder AddBackofficeData(this IHostApplicationBuilder builder)
    {
        // Add PostgreSQL DataSource for Marten
        builder.AddNpgsqlDataSource("backofficedb");

        // Setup Marten
        builder.Services.AddMarten(opts =>
        {
            opts.Connection(builder.Configuration.GetConnectionString("backofficedb")!);
            opts.Schema.For<ContentItem>().NgramIndex(x => x.SearchText);

            // Align Marten with System.Text.Json enum settings used by the API
            opts.Serializer(new Marten.Services.SystemTextJsonSerializer
            {
                EnumStorage = Weasel.Core.EnumStorage.AsString
            });
        })
        .UseLightweightSessions()
        .IntegrateWithWolverine(); // Reverting to original for now

        return builder;
    }

    public static IHostBuilder AddBackofficeMessaging(this IHostBuilder host, IConfiguration configuration, params Assembly[] additionalAssemblies)
    {
        host.UseWolverine(opts =>
        {
            opts.UseRabbitMq(configuration.GetConnectionString("messaging")!)
                .AutoProvision()
                .UseConventionalRouting();

            // Include any additional assemblies provided (e.g. Domain/Logic assemblies)
            foreach (var assembly in additionalAssemblies)
            {
                opts.Discovery.IncludeAssembly(assembly);
            }

            opts.Policies.UseDurableLocalQueues();
        });

        return host;
    }
}

