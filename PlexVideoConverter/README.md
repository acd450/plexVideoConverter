# Plex Video Converter

This is the README.md for the Plex Video Converter project. I created this tool to upconvert my .264 videos to .265
to save space on my plex server without losing too much, if any in quality.

Using a file listener service, whenever a .mp4 or .mkv file is dropped in the specified directory, it will trigger
a conversion.  The fileListenerSettings.json is what sets up the file listener service.  The FfmpegCoreService
will handle the actual video conversion.

## appsettings.json
```
"FfmpegSettings": {
    "videoQuality": 22,
    "reportPercentProgress": 10
  }
```
The appsetttings.json must contain this section.
Video quality relates to the CRF in Ffmpeg.
Report Percent Progress is the % amount before it will log its progress.  Keeps it from logging every .01%

## File Listener Settings

1. FolderID (string)
Unique identifier of the combination File type/folder
Arbitrary number (for instance 001, 002, and so on)

2. FolderEnabled (boolean)
If TRUE: the file type and folder will be monitored, otherwise ignored

3. FolderType (string)
Type of folder: EXPORT, IMPORT, POST-IMPORT

4. FolderDescription (string)
Description of the type of files and folder location – Just for documentation purpose

5. FolderFilter (string)
Filter to select the type of files to be monitored.
(Examples: *.mkv, *.*, Ted.Lasso.S1*.mp4)

6. FolderPath (string)
Full path to be monitored (i.e.: D:\files\movies\TedLasso\ )

7. FolderIncludeSub (boolean)
If TRUE: the folder and its subfolders will be monitored

8. ExecutableFile (string)
Specifies the command or action to be executed after an event has raised

9. ExecutableArguments (string)
List of arguments to be passed to the executable file


