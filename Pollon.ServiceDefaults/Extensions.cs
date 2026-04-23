using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using Npgsql;
using Winton.Extensions.Configuration.Consul;
using Winton.Extensions.Configuration.Consul.Parsers;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";


    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Suppress noisy database command logs
        builder.Logging.AddFilter("Npgsql.Command", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        builder.Logging.AddFilter("Wolverine", LogLevel.Warning);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddSource("Wolverine") // Capture Wolverine messaging traces
                    .AddAspNetCoreInstrumentation(tracing =>
                    {
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath);
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation() // Trace EF Core database calls
                    .AddNpgsql()
                    .SetSampler(new NoisySpansSampler(new AlwaysOnSampler()))
                    .AddProcessor(new NoisySpansProcessor());
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var useOtlpExporter = !string.IsNullOrWhiteSpace(endpoint);

        var jaegerEndpoint = builder.Configuration["JAEGER_OTLP_ENDPOINT"];
        var useJaeger = !string.IsNullOrWhiteSpace(jaegerEndpoint);

        if (useOtlpExporter || useJaeger)
        {
            if (useOtlpExporter)
            {
                builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());
            }

            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    if (useOtlpExporter) tracing.AddOtlpExporter("aspire", _ => { });
                    if (useJaeger) tracing.AddOtlpExporter("jaeger", options =>
                    {
                        options.Endpoint = new Uri(jaegerEndpoint!);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
                })
                .WithMetrics(metrics =>
                {
                    if (useOtlpExporter) metrics.AddOtlpExporter("aspire", _ => { });
                    if (useJaeger) metrics.AddOtlpExporter("jaeger", options =>
                    {
                        options.Endpoint = new Uri(jaegerEndpoint!);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
                });
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }

    public static TBuilder AddConsulConfiguration<TBuilder>(this TBuilder builder, string keyPrefix = "pollon") where TBuilder : IHostApplicationBuilder
    {
        var consulAddress = builder.Configuration["CONSUL_URL"] ?? builder.Configuration["CONSUL_HTTP_ADDR"] ?? "http://localhost:8500";

        builder.Configuration.AddConsul(
            keyPrefix,
            options =>
            {
                options.ConsulConfigurationOptions = consulConfig =>
                {
                    consulConfig.Address = new Uri(consulAddress);
                };
                options.Parser = new SimpleConfigurationParser();
                options.ReloadOnChange = true;
                options.Optional = true;
            });

        return builder;
    }
}

/// <summary>
/// Custom Sampler to drop noisy background spans from Wolverine, Marten, and PostgreSQL polling.
/// </summary>
internal class NoisySpansSampler : Sampler
{
    private readonly Sampler _innerSampler;

    public NoisySpansSampler(Sampler innerSampler)
    {
        _innerSampler = innerSampler;
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        var spanName = samplingParameters.Name ?? "";

        // Filter out Wolverine background tasks by span name (if known at start)
        if (spanName.Contains("wolverine.persistence", StringComparison.OrdinalIgnoreCase) ||
            spanName.Contains("wolverine.polling", StringComparison.OrdinalIgnoreCase))
        {
            return new SamplingResult(SamplingDecision.Drop);
        }

        return _innerSampler.ShouldSample(samplingParameters);
    }
}

/// <summary>
/// Custom Processor to drop noisy Npgsql and Wolverine spans by checking names and tags.
/// </summary>
internal class NoisySpansProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Check both the span name (for Wolverine/Marten internal tasks) 
        // and the SQL query text (for Npgsql spans).
        if (IsNoisy(activity.DisplayName) || 
            IsNoisy(activity.GetTagItem("db.query.text") as string) || 
            IsNoisy(activity.GetTagItem("db.statement") as string))
        {
            // Setting flags to None effectively drops the span from being exported by most Otel exporters
            activity.ActivityTraceFlags = ActivityTraceFlags.None;
        }
    }

    private static bool IsNoisy(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        var lower = text.ToLowerInvariant();
        return lower.Contains("wolverine_") || 
               lower.Contains("mt_") || 
               lower.Contains("pg_catalog") ||
               lower.Contains("wolverine.persistence") ||
               lower.Contains("wolverine.polling") ||
               lower.Contains("mt_node_config") ||
               lower.Contains("advisory");
    }
}
