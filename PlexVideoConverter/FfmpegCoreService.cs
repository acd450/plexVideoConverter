using FFMpegCore;
using FFMpegCore.Enums;

namespace PlexVideoConverter;

public class FfmpegCoreService
{
    private static FfmpegCoreService _instance;
    public static FfmpegCoreService Instance => _instance ??= new FfmpegCoreService();

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
            await taskGenerator();
        }
        finally
        {
            sem.Release();
        }
    }
    
    public Task ConvertVideoAsync(string inputFile)
    {
        var outputName =
            FileListenerService.Instance.GetExportSettings()
                .Find(s => s.FolderID == "VideoOutputFiles")?
                .FolderPath;

        var outputFileName = inputFile.Substring(inputFile.LastIndexOf("\\", StringComparison.Ordinal),
                inputFile.Length - inputFile.LastIndexOf("\\", StringComparison.Ordinal))
            .Replace(".mp4", ".mkv");
        
        return FFMpegArguments.FromFileInput(inputFile)
            .OutputToFile(outputName + outputFileName, false, options => options
                .WithVideoCodec(VideoCodec.LibX265)
                .WithConstantRateFactor(24)
                .WithFastStart())
            .ProcessAsynchronously();
    }

    public void CompleteFileConversion(string fullPathFile)
    {
        var fileName = fullPathFile.Substring(fullPathFile.LastIndexOf("\\", StringComparison.Ordinal),
            fullPathFile.Length - fullPathFile.LastIndexOf("\\", StringComparison.Ordinal));
        var outputName =
            FileListenerService.Instance.GetExportSettings()
                .Find(s => s.FolderID == "PostConvertOriginalFiles")?
                .FolderPath;
        File.Move(fullPathFile, outputName + fileName);
    }
    
    public static void ConvertVideoWithCustomArgs(string args)
    {
        var inputName = "C:\\Users\\user\\Videos\\ffmpeg-ToConvert\\Keystone Instagram.mp4";
        var outputName = "C:\\Users\\user\\Videos\\ffmpeg-ToConvert\\Keystone Instagram-sm.mkv";
        
        try
        {
            FFMpegArguments.FromFileInput(inputName)
                .OutputToFile(outputName, true, options => options
                    .WithCustomArgument(args))
                .ProcessSynchronously();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}