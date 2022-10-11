# plexVideoConverter
The Plex Video converter is built to upgrade h.264 videos that take up a lot of space and upgrade them
to the newer h.265 format.  Hopefully saving space on your NAS when it comes to storing movies, shows, or home video
without sacrificing video quality.

## Setup
1. How to setup fileListenerSettings.
   * Used to setup file watching service that looks for new files to import and tell the service where to export them to.
   ```
   * FolderID: 
   * FolderEnabled: "True" or "False", Will this folder setting be enabled
   * FolderType: "IMPORT", "POST-IMPORT", or "EXPORT", Use this folder setting for imports, post imports, or exports
   * FolderDescription: Just a string description about this folder setting
   * FolderFilter: ".mkv" or ".mp4" are the most common. Used for listening for specific file types
   * FolderPath: The path to where to listen for import files, or where to export converted files to
   * FolderIncludeSub: untested
   * ExecutableFile: unused
   * ExecutableArguments: unused
   ```
2. fileListenerSettings are stored in C:/ProgramData/PlexVideoConverter/fileListenerSettings.json.  An example is in this project.
3. Log files are stored in C:/ProgramData/PlexVideoConverter/Logs/

## FFMPEG testing
* I have avoided using nvidia GPUs to convert the videos, it is much quicker, but the quality isn't as good and it takes up more space.
* Using the CPU for the conversion seems to be the best way.
* Highly recommend running this on at least a quad core CPU.
* On the 8 core intel CPU, it runs about 2.5s of video conversion per second for 1080p video.