using System.Runtime.InteropServices;
using NLog;

namespace PlexVideoConverter;

public class FileListenerService
{
    private static Logger logger = LogManager.GetCurrentClassLogger();
    
    private static FileListenerService _instance;
    public static FileListenerService Instance => _instance == null ? new FileListenerService() : _instance;

    private List<FileSystemWatcher> _listFileSystemWatcher = new();
    
    const int ERROR_SHARING_VIOLATION = 32;
    const int ERROR_LOCK_VIOLATION = 33;

    public FileListenerService()
    {
        SettingsService.Instance.PopulateGlobalSettings();
    }

    public void StartFileSystemWatcher()
    {
        if (SettingsService.Instance.FileListenerSettings.Count is 0)
        {
            logger.Info("There are no file listener settings.  Missing fileListenerSettings.json");
            return;
        }
        
        // Creates a new instance of the list
        Instance._listFileSystemWatcher = new List<FileSystemWatcher>();
        // Loop the list to process each of the folder specifications found
        var importSettings = SettingsService.Instance.GetImportSettings();
        if (importSettings == null) throw new Exception("fileListenerSettings.json not found. Exiting.");
        foreach (FileListenerSettings setting in importSettings)
        {
            logger.Info($"Initializing File watcher for Type:" +
                        $" {setting.FolderType} Path: {setting.FolderPath}");
            
            DirectoryInfo dir = new DirectoryInfo(setting.FolderPath);
            // Checks whether the folder is enabled and
            // also the directory is a valid location
            if (setting.FolderEnabled && dir.Exists)
            {
                // Creates a new instance of FileSystemWatcher
                FileSystemWatcher fileSWatch = new FileSystemWatcher();
                // Sets the filter
                fileSWatch.Filter = setting.FolderFilter;
                // Sets the folder location
                fileSWatch.Path = setting.FolderPath;
                // Subscribe to notify filters
                fileSWatch.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName |
                                          NotifyFilters.DirectoryName | NotifyFilters.CreationTime;
                // Associate the event that will be triggered when a new file
                // is added to the monitored folder, using a lambda expression                   
                fileSWatch.Created += fileSWatch_Created;
                // Begin watching
                fileSWatch.EnableRaisingEvents = true;
                // Add the systemWatcher to the list
                Instance._listFileSystemWatcher.Add(fileSWatch);
                // Record a log entry into Windows Event Log
                logger.Info(
                    $"Starting to monitor files with extension ({fileSWatch.Filter}) in the folder ({fileSWatch.Path})");
            }
        }
    }

    /// <summary>This event is triggered when a file with the specified
    /// extension is created on the monitored folder.  Important, this
    /// method must be low cost, otherwise it kills the file watch.</summary>
    /// <param name="sender">Object raising the event</param>
    /// <param name="e">List of arguments - FileSystemEventArgs</param>
    private void fileSWatch_Created(object sender, FileSystemEventArgs e)
    {
        logger.Debug($"fileSWatch_Created() with file: {e.FullPath}");
        var fileName = e.FullPath;

        //Lets wait till the file is fully transferred before we do anything
        while (IsFileLocked(fileName))
        {
            logger.Info($"File: {fileName} is locked, waiting 2 seconds before attempting to queue.");
            Thread.Sleep(2000);
        }
        
        Task.Run(() => FfmpegCoreService.Instance.AddItems(fileName));
        logger.Debug($"{fileName} has been queued.");
    }
    
    /// <summary>
    /// Check if the file has a lock on it.  If it does the file is still being transferred to the disk
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static bool IsFileLocked(string file)
    {
        //check that problem is not in destination file
        if (File.Exists(file) != true) return false;
        FileStream? stream = null;
        try
        {
            stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (Exception ex2)
        {
            //_log.WriteLog(ex2, "Error in checking whether file is locked " + file);
            var errorCode = Marshal.GetHRForException(ex2) & ((1 << 16) - 1);
            if ((ex2 is IOException) && errorCode is ERROR_SHARING_VIOLATION or ERROR_LOCK_VIOLATION)
            {
                return true;
            }
        }
        finally
        {
            stream?.Close();
        }
        return false;
    }
}