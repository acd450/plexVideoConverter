using NLog;
using NLog.Extensions.Logging;

namespace PlexVideoConverter
{
    public class Startup
    {
        public static IConfigurationRoot Config { get; set; }
        public static FfmpegSettings FfmpegSettings { get; private set; }
        public const string FfmpegSettingsLocation = "C:\\ProgramData\\PlexVideoConverter";
        public static async Task Main(string[] args)
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            
            NLogProviderOptions nlpopts = new NLogProviderOptions
            {
                IgnoreEmptyEventId = true,
                CaptureMessageTemplates = true,
                CaptureMessageProperties = true,
                ParseMessageTemplates = true,
                IncludeScopes = true,
                ShutdownOnDispose = true
            };

            IHost host = Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = ".NET PlexVideoConverter Service";
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<FileListenerService>();
                    services.AddSingleton<FfmpegCoreService>();
                })
                .ConfigureAppConfiguration(builder =>
                {
                    builder.Sources.Clear();
                    builder.AddConfiguration(Config);
                })
                .ConfigureLogging((context, logging) =>
                {
                    //Ensure logging directory exists
                    Directory.CreateDirectory(@"C:\\ProgramData\\PlexVideoConverter\\Logs\\");

                    logging.ClearProviders();
                    logging.AddNLog(new NLogLoggingConfiguration(Config.GetSection("NLog")));
                })
                .Build();

            FfmpegSettings = Config.GetSection("FfmpegSettings").Get<FfmpegSettings>();

            await host.RunAsync();
        }
    }
}
