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
    public static void TestConvertVideo()
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
        try
        {
            var outputPath =
                FileListenerService.Instance.GetExportSettings().FirstOrDefault()?
                    .FolderPath;

            var outputFileName = inputFile.Substring(inputFile.LastIndexOf("\\", StringComparison.Ordinal),
                    inputFile.Length - inputFile.LastIndexOf("\\", StringComparison.Ordinal))
                .Replace(".mp4", ".mkv");

            logger.Info($"Converting File: {inputFile}");
            logger.Info($"Output File: {outputPath + outputFileName}");

            var percentTracker = 0;

            void ProgressHandler(double p)
            {
                //Only log when the percent exceeds the reportPercentCompletion
                if (percentTracker < p / Startup.FfmpegSettings.reportPercentProgress)
                {
                    logger.Info("Current Video Progress: " + p + "%");
                    percentTracker = (int)Math.Ceiling(p / Startup.FfmpegSettings.reportPercentProgress);
                }
            }

            var videoDuration = FFProbe.Analyse(inputFile).Duration;
            
            logger.Info($"Ffmpeg has crf={Startup.FfmpegSettings.videoQuality}");
            
            return FFMpegArguments
                .FromFileInput(inputFile)
                .OutputToFile(outputPath + outputFileName, false, options => options
                    .WithVideoCodec(VideoCodec.LibX265)
                    .WithConstantRateFactor(Startup.FfmpegSettings.videoQuality)
                    .WithFastStart())
                .NotifyOnProgress(ProgressHandler, videoDuration)
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            logger.Error("Error during video conversion. " + ex.Message, ex);
            return Task.CompletedTask;
        }
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
        
        CalculateConversionStats(inputFilePath);
        
        logger.Info($"Finished converting video, moving to: {completedPath + fileName}");
        
        File.Move(inputFilePath, completedPath + fileName);
    }

    private void CalculateConversionStats(string inputFilePath)
    {
        var outputPath =
            FileListenerService.Instance.GetExportSettings().FirstOrDefault()?
                .FolderPath;

        var outputFileName = inputFilePath.Substring(inputFilePath.LastIndexOf("\\", StringComparison.Ordinal),
                inputFilePath.Length - inputFilePath.LastIndexOf("\\", StringComparison.Ordinal))
            .Replace(".mp4", ".mkv");

        var fiInput = new FileInfo(inputFilePath);
        var fiOutput = new FileInfo(outputPath + outputFileName);

        if (!fiInput.Exists)
        {
            logger.Error("Cannot find Input file for final stats. File Path: " + inputFilePath);
            return;
        } if (!fiOutput.Exists)
        {
            logger.Error("Cannot find Output file for final stats. File Path: " + outputPath + outputFileName);
            return;
        }

        var inputFileSize = fiInput.Length >> 20;
        var outputFileSize = fiOutput.Length >> 20;
        var savingsPercent = ((double)inputFileSize - outputFileSize) / inputFileSize * 100;
        logger.Info("Stats for file: " + fiOutput.Name);
        logger.Info("Input File Size: " + inputFileSize + " MB");
        logger.Info("Output File Save: " + outputFileSize + " MB");
        logger.Info("Total conversion savings: " + Math.Floor(savingsPercent) + "%");
    }
}