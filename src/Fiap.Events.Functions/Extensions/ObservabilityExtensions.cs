using Fiap.Events.Functions.Utils;
using Serilog.Sinks.Grafana.Loki;

namespace Fiap.Events.Functions.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddProxyObservability(this IServiceCollection services, IConfiguration configuration, string environmentName)
    {
        ConfigureProxySerilog(services, configuration, environmentName);
        ConfigureProxyOpenTelemetry(services, configuration);
        return services;
    }

    private static void ConfigureProxySerilog(IServiceCollection services, IConfiguration configuration, string environmentName)
    {
        services.AddOptions<Utils.Serilog>()
            .Bind(configuration.GetSection(nameof(Utils.Serilog)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var lokiUri = configuration["Serilog:WriteTo:1:Args:Uri"];

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration.GetSection("Serilog"))
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithProperty("Application", "Lambda")
            .WriteTo.Console();

        if (!string.IsNullOrEmpty(lokiUri))
        {
            try
            {
                loggerConfig.WriteTo.GrafanaLoki(
                    uri: lokiUri,
                    labels: new List<LokiLabel>
                    {
                        new() { Key = "app-events", Value = "dotnet-lambda" },
                        new() { Key = "env", Value = environmentName }
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not configure Loki sink: {ex.Message}");
            }
        }

        Log.Logger = loggerConfig.CreateLogger();
    }

    private static void ConfigureProxyOpenTelemetry(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DistributedTracing>()
            .Bind(configuration.GetSection(nameof(DistributedTracing)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var serviceName = configuration["DistributedTracing:Jaeger:ServiceName"];
        var endpoint = configuration["DistributedTracing:Jaeger:Endpoint"];
        var host = configuration["DistributedTracing:Jaeger:Host"];
        var portString = configuration["DistributedTracing:Jaeger:Port"];

        if (!string.IsNullOrEmpty(serviceName))
        {
            try
            {
                services.AddOpenTelemetry()
                    .WithTracing(tracing => tracing
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
                        .AddSource("AWS.Lambda.Powertools")
                        .AddJaegerExporter(options =>
                        {
                            if (!string.IsNullOrEmpty(endpoint))
                            {
                                options.Endpoint = new Uri(endpoint);
                                options.Protocol = JaegerExportProtocol.HttpBinaryThrift;
                            }
                            else if (!string.IsNullOrEmpty(host) && int.TryParse(portString, out var port))
                            {
                                options.AgentHost = host;
                                options.AgentPort = port;
                            }
                        }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not configure OpenTelemetry: {ex.Message}");
            }
        }
    }
}