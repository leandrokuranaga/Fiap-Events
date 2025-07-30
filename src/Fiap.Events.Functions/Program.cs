namespace Fiap.Events.Functions
{
    public static partial class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        static Program()
        {
            try
            {
                Console.WriteLine("=== INITIALIZING SERVICE PROVIDER ===");

                var builder = new ConfigurationBuilder();

                var currentDir = Directory.GetCurrentDirectory();
                var possiblePaths = new[]
                {
                    currentDir,
                    Path.Combine(currentDir, "bin"),
                    "/var/task",
                    AppDomain.CurrentDomain.BaseDirectory
                };

                Console.WriteLine($"Current directory: {currentDir}");
                Console.WriteLine($"Base directory: {AppDomain.CurrentDomain.BaseDirectory}");

                foreach (var path in possiblePaths)
                {
                    builder.SetBasePath(path);
                    var appsettingsPath = Path.Combine(path, "appsettings.json");

                    if (File.Exists(appsettingsPath))
                    {
                        Console.WriteLine($"Found appsettings.json at: {appsettingsPath}");
                        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"appsettings.json not found at: {appsettingsPath}");
                    }
                }

                builder.AddEnvironmentVariables();

                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                Console.WriteLine($"Environment: {environmentName}");

                try
                {
                    builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load appsettings.{environmentName}.json: {ex.Message}");
                }

                var configuration = builder.Build();

                Console.WriteLine("=== CONFIGURATION DEBUG ===");
                var serviceBusQueueName = configuration["ServiceBus:QueueName"];
                Console.WriteLine($"ServiceBus:QueueName = {serviceBusQueueName ?? "NULL"}");

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

                Console.WriteLine("=== SERVICE PROVIDER INITIALIZED SUCCESSFULLY ===");

                try
                {
                    var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("StartupLogger");
                    logger.LogInformation("Logger service resolved successfully");

                    var eventService = ServiceProvider.GetRequiredService<IEventService>();
                    Console.WriteLine("EventService resolved successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error resolving services: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR in Program static constructor: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }

    [ExcludeFromCodeCoverage]
    public partial class Program { }
}