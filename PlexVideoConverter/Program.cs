using NLog;
using NLog.Extensions.Logging;
using PlexVideoConverter;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

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
        builder.AddConfiguration(config);
    })
    .ConfigureLogging((context, logging) =>
    {
        //Ensure logging directory exists
        Directory.CreateDirectory(@"C:\\ProgramData\\PlexVideoConverter\\Logs\\");
        
        logging.AddNLog(new NLogLoggingConfiguration(config.GetSection("NLog")));
    })
    .Build();

await host.RunAsync();