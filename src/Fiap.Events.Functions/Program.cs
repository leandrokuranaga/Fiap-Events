using Microsoft.Extensions.Logging;

namespace Fiap.Events.Functions
{
    public static partial class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        static Program()
        {
            ILogger logger = null;
            
            try
            {
                var tempServices = new ServiceCollection();
                tempServices.AddLogging(configure =>
                {
                    configure.AddConsole();
                    configure.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                });
                var tempProvider = tempServices.BuildServiceProvider();
                var loggerFactory = tempProvider.GetRequiredService<ILoggerFactory>();
                logger = loggerFactory.CreateLogger("Program");

                logger.LogInformation("=== INITIALIZING SERVICE PROVIDER ===");

                var builder = new ConfigurationBuilder();

                var currentDir = Directory.GetCurrentDirectory();
                var possiblePaths = new[]
                {
                    currentDir,
                    Path.Combine(currentDir, "bin"),
                    "/var/task",
                    AppDomain.CurrentDomain.BaseDirectory
                };

                logger.LogInformation("Current directory: {CurrentDir}", currentDir);
                logger.LogInformation("Base directory: {BaseDir}", AppDomain.CurrentDomain.BaseDirectory);

                foreach (var path in possiblePaths)
                {
                    builder.SetBasePath(path);
                    var appsettingsPath = Path.Combine(path, "appsettings.json");

                    if (File.Exists(appsettingsPath))
                    {
                        logger.LogInformation("Found appsettings.json at: {AppsettingsPath}", appsettingsPath);
                        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                        break;
                    }
                    else
                    {
                        logger.LogInformation("appsettings.json not found at: {AppsettingsPath}", appsettingsPath);
                    }
                }

                builder.AddEnvironmentVariables();

                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                logger.LogInformation("Environment: {EnvironmentName}", environmentName);

                try
                {
                    builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Could not load appsettings.{EnvironmentName}.json: {ErrorMessage}", environmentName, ex.Message);
                }

                var configuration = builder.Build();

                logger.LogInformation("=== CONFIGURATION DEBUG ===");
                var serviceBusQueueName = configuration["ServiceBus:QueueName"];
                logger.LogInformation("ServiceBus:QueueName = {QueueName}", serviceBusQueueName ?? "NULL");

                var services = new ServiceCollection();

                services.AddSingleton<IConfiguration>(configuration);

                services.AddLogging(configure =>
                {
                    configure.AddConsole();
                    configure.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                });

                services.Configure<ServiceBusOptions>(configuration.GetSection("ServiceBus"));
                services.Configure<AppSettings>(configuration);

                services.AddTransient<IEventService, EventService>();
                services.AddTransient<IEmailService, EmailService>();

                ServiceProvider = services.BuildServiceProvider();

                logger.LogInformation("=== SERVICE PROVIDER INITIALIZED SUCCESSFULLY ===");

                try
                {
                    var loggerFactory2 = ServiceProvider.GetRequiredService<ILoggerFactory>();
                    var startupLogger = loggerFactory2.CreateLogger("StartupLogger");
                    startupLogger.LogInformation("Logger service resolved successfully");

                    var eventService = ServiceProvider.GetRequiredService<IEventService>();
                    logger.LogInformation("EventService resolved successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error resolving services: {ErrorMessage}", ex.Message);
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.LogCritical(ex, "FATAL ERROR in Program static constructor: {ErrorMessage}", ex.Message);
                }
                else
                {
                    Console.WriteLine($"FATAL ERROR in Program static constructor: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                throw;
            }
        }
    }

    [ExcludeFromCodeCoverage]
    public partial class Program { }
}