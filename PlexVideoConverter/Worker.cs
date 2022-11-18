using NLog;

namespace PlexVideoConverter;

public class Worker : BackgroundService
{
    private static Logger logger = LogManager.GetCurrentClassLogger();

    public Worker(FileListenerService fileListenerService,
        FfmpegCoreService ffmpegCoreService)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            FileListenerService.Instance.StartFileSystemWatcher();
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{Message}", ex.Message);
            Environment.Exit(1);
        }
        
    }
}