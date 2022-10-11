namespace PlexVideoConverter;

public class Worker : BackgroundService
{
    private readonly FileListenerService _fileListenerService;
    private readonly FfmpegCoreService _ffmpegCoreService;
    private readonly ILogger<Worker> _logger;

    public Worker(FileListenerService fileListenerService,
        FfmpegCoreService ffmpegCoreService,
        ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            FileListenerService.Instance.StartFileSystemWatcher();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            Environment.Exit(1);
        }
        
    }
}