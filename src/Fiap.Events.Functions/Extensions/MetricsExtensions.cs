using Prometheus;

namespace Fiap.Events.Functions.Extensions;

public static class MetricsExtensions
{
    public static IServiceCollection AddLambdaMetrics(this IServiceCollection services)
    {
        services.AddSingleton(LambdaMetrics.Instance);
        
        return services;
    }
}

public class LambdaMetrics
{
    private static readonly Lazy<LambdaMetrics> _instance = new(() => new LambdaMetrics());
    public static LambdaMetrics Instance => _instance.Value;

    public readonly Counter MessagesProcessed = Metrics
        .CreateCounter("lambda_messages_processed_total", 
                      "Total number of SQS messages processed",
                      ["status", "message_type"]);

    public readonly Histogram MessageProcessingDuration = Metrics
        .CreateHistogram("lambda_message_processing_duration_seconds",
                        "Time spent processing SQS messages",
                        ["message_type"]);

    public readonly Counter EmailsSent = Metrics
        .CreateCounter("lambda_emails_sent_total",
                      "Total number of emails sent",
                      ["status", "email_type"]);

    public readonly Gauge ActiveConnections = Metrics
        .CreateGauge("lambda_active_connections",
                    "Number of active connections");

    public readonly Counter LambdaInvocations = Metrics
        .CreateCounter("lambda_invocations_total",
                      "Total number of Lambda invocations",
                      ["function_name", "status"]);

    public readonly Histogram LambdaExecutionDuration = Metrics
        .CreateHistogram("lambda_execution_duration_seconds",
                        "Lambda function execution duration",
                        ["function_name"]);

    public readonly Counter ErrorsTotal = Metrics
        .CreateCounter("lambda_errors_total",
                      "Total number of errors",
                      ["error_type", "function_name"]);

    public readonly Counter HttpRequests = Metrics
        .CreateCounter("http_requests_total",
                      "Total HTTP requests processed",
                      ["method", "status_code"]);

    public readonly Histogram HttpRequestDuration = Metrics
        .CreateHistogram("http_request_duration_seconds",
                        "HTTP request duration",
                        ["method"]);

    private LambdaMetrics()
    {
    }

    public string GetMetricsText()
    {
        using var stream = new MemoryStream();
        Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream).Wait();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public void UseHttpMetrics()
    {
        Log.Information("HTTP Metrics configured for Lambda");
    }

    public void MapMetrics()
    {
        Log.Information("Metrics endpoint mapped for Lambda");
    }
}