using FFMpegCore;
using FFMpegCore.Enums;
using NLog;

namespace PlexVideoConverter;

public class FfmpegCoreService
{
    private static FfmpegCoreService _instance;
    public static FfmpegCoreService Instance => _instance ??= new FfmpegCoreService();
    
    private static Logger logger = LogManager.GetCurrentClassLogger();

    private SemaphoreSlim sem;
    
    public FfmpegCoreService()
    {
        sem = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Test function to see if FFMPEG was working
    /// </summary>
    public static void ConvertVideo()
    {
        var inputName = "C:\\Users\\user\\Videos\\ffmpeg-ToConvert\\Keystone Instagram.mp4";
        var outputName = "C:\\Users\\user\\Videos\\ffmpeg-ToConvert\\Keystone Instagram-sm.mkv";
        
        try
        {
            FFMpegArguments.FromFileInput(inputName)
                .OutputToFile(outputName, false, options => options
                    .WithVideoCodec(VideoCodec.LibX265)
                    .WithConstantRateFactor(24)
                    .WithFastStart())
                .ProcessSynchronously();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    
    public async Task Enqueue(Func<Task> taskGenerator)
    {
        await sem.WaitAsync();
        try
        {
            logger.Info("Tasked started...");
            await taskGenerator();
        }
        finally
        {
            logger.Info("Task finished processing...");
            sem.Release();
        }
    }
    
    public Task ConvertVideoAsync(string inputFile)
    {
        var outputPath =
            FileListenerService.Instance.GetExportSettings().FirstOrDefault()?
                .FolderPath;

        var outputFileName = inputFile.Substring(inputFile.LastIndexOf("\\", StringComparison.Ordinal),
                inputFile.Length - inputFile.LastIndexOf("\\", StringComparison.Ordinal))
            .Replace(".mp4", ".mkv");
        
        logger.Info($"Converting File: {inputFile}");
        logger.Info($"Output File: {outputPath + outputFileName}");
        
        return FFMpegArguments.FromFileInput(inputFile)
            .OutputToFile(outputPath + outputFileName, false, options => options
                .WithVideoCodec(VideoCodec.LibX265)
                .WithConstantRateFactor(24)
                .WithFastStart())
            .ProcessAsynchronously();
    }

    /// <summary>
    /// Anything to run after the video conversion is complete. Currently moves files to a 
    /// </summary>
    /// <param name="fullPathFile"></param>
    public void CompleteFileConversion(string inputFilePath)
    {
        var fileName = inputFilePath.Substring(inputFilePath.LastIndexOf("\\", StringComparison.Ordinal),
            inputFilePath.Length - inputFilePath.LastIndexOf("\\", StringComparison.Ordinal));
        
        var completedPath =
            FileListenerService.Instance.GetPostImportSettings()?
                .FolderPath;
        
        logger.Info($"Finished converting video, moving to: {completedPath + fileName}");
        
        File.Move(inputFilePath, completedPath + fileName);
    }
}