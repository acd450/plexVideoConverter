using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace PlexVideoConverter;

public class FileListenerService
{
    private static FileListenerService _instance;
    public static FileListenerService Instance => _instance == null ? new FileListenerService() : _instance;

    private List<FileListenerSettings> FileListenerSettings = new();

    private List<FileSystemWatcher> _listFileSystemWatcher = new();
    
    const int ERROR_SHARING_VIOLATION = 32;
    const int ERROR_LOCK_VIOLATION = 33;

    public FileListenerService()
    {
        PopulateGlobalSettings();
    }

    private void PopulateGlobalSettings()
    {
        try
        {
            var globalSettingsPath = Assembly.GetExecutingAssembly().Location;
            using (StreamReader r = new StreamReader(Path.GetDirectoryName(globalSettingsPath) + "\\fileListenerSettings.json"))
            {
                string json = r.ReadToEnd();
                var fileListenerSettings = JsonConvert.DeserializeObject<List<FileListenerSettings>>(json);
                if (fileListenerSettings != null)
                    FileListenerSettings = fileListenerSettings;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in GlobalSettingsService. " + ex.Message);
        }
    }

    public void StartFileSystemWatcher()
    {
        if (Instance.FileListenerSettings.Count is 0)
            return;
        
        // Creates a new instance of the list
        Instance._listFileSystemWatcher = new List<FileSystemWatcher>();
        // Loop the list to process each of the folder specifications found
        var importSettings = GetImportSettings();
        if (importSettings == null) return;
        foreach (FileListenerSettings setting in importSettings)
        {
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
                                          NotifyFilters.DirectoryName;
                // Associate the event that will be triggered when a new file
                // is added to the monitored folder, using a lambda expression                   
                fileSWatch.Created += (senderObj, fileSysArgs) =>
                    fileSWatch_Created(senderObj, fileSysArgs);
                // Begin watching
                fileSWatch.EnableRaisingEvents = true;
                // Add the systemWatcher to the list
                Instance._listFileSystemWatcher.Add(fileSWatch);
                // Record a log entry into Windows Event Log
                Console.WriteLine(
                    $"Starting to monitor files with extension ({fileSWatch.Filter}) in the folder ({fileSWatch.Path})");
            }
        }
    }

    public List<FileListenerSettings> GetImportSettings()
    {
        return Instance.FileListenerSettings.FindAll(setting => setting.FolderType == "IMPORT");
    }

    public List<FileListenerSettings> GetExportSettings()
    {
        return Instance.FileListenerSettings.FindAll(setting => setting.FolderType == "EXPORT");
    }
    
    /// <summary>This event is triggered when a file with the specified
    /// extension is created on the monitored folder</summary>
    /// <param name="sender">Object raising the event</param>
    /// <param name="e">List of arguments - FileSystemEventArgs</param>
    /// <param name="action_Exec">The action to be executed upon detecting a change in the File system</param>
    /// <param name="action_Args">arguments to be passed to the executable (action)</param>
    private async void fileSWatch_Created(object sender, FileSystemEventArgs e)
    {
        var fileName = e.FullPath;

        //Lets wait till the file is fully transferred before we do anything
        while (IsFileLocked(fileName))
        {
            Thread.Sleep(2000);
        }
        
        await FfmpegCoreService.Instance.Enqueue(() => FfmpegCoreService.Instance.ConvertVideoAsync(fileName));
        FfmpegCoreService.Instance.CompleteFileConversion(fileName);
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