using Amazon;
using Amazon.Extensions.NETCore.Setup;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
                tempServices.AddLogging(cfg => {
                    cfg.AddConsole();
                    cfg.SetMinimumLevel(LogLevel.Information);
                });
                var tempProvider = tempServices.BuildServiceProvider();
                var loggerFactory = tempProvider.GetRequiredService<ILoggerFactory>();
                logger = loggerFactory.CreateLogger("Program");

                logger.LogInformation("=== INITIALIZING SERVICE PROVIDER ===");

                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                var isDevelopment = environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase);
                logger.LogInformation("Environment: {EnvironmentName}", environmentName);

                var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? "sa-east-1";
                var awsOptions = new AWSOptions { Region = RegionEndpoint.GetBySystemName(awsRegion) };
                
                var builder = new ConfigurationBuilder();

                var currentDir = Directory.GetCurrentDirectory();
                var possiblePaths = new[]
                {
                    currentDir,
                    Path.Combine(currentDir, "bin"),
                    "/var/task",
                    AppDomain.CurrentDomain.BaseDirectory
                };

                string basePath = currentDir;
                foreach (var path in possiblePaths)
                {
                    var appsettingsPath = Path.Combine(path, "appsettings.json");
                    if (File.Exists(appsettingsPath))
                    {
                        logger.LogInformation("Found appsettings.json at: {AppsettingsPath}", appsettingsPath);
                        basePath = path;
                        break;
                    }
                }

                builder.SetBasePath(basePath);

                var ssmPath = Environment.GetEnvironmentVariable("SSM_PATH") ?? "/myapp/prod/";
                builder.AddSystemsManager(source =>
                {
                    source.Path = ssmPath;
                    source.Optional = true;
                    source.ReloadAfter = TimeSpan.FromMinutes(5);
                    source.AwsOptions = awsOptions;
                });

                if (isDevelopment)
                {
                    builder.AddUserSecrets(typeof(Program).Assembly, optional: true, reloadOnChange: true);
                }

                builder
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

                builder.AddEnvironmentVariables();

                var configuration = builder.Build();

                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(configuration);
                services.AddDefaultAWSOptions(awsOptions);

                services.AddLogging(cfg => {
                    cfg.AddConsole();
                    cfg.SetMinimumLevel(LogLevel.Information);
                });

                services.Configure<ServiceBusOptions>(configuration.GetSection("ServiceBus"));
                services.Configure<AppSettings>(configuration);

                services.AddTransient<IEventService, EventService>();
                services.AddTransient<IEmailService, EmailService>();

                ServiceProvider = services.BuildServiceProvider();

                logger.LogInformation("=== SERVICE PROVIDER INITIALIZED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogCritical(ex, "FATAL ERROR in Program static constructor: {ErrorMessage}", ex.Message);
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
