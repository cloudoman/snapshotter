using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Cloudoman.AwsTools.Snapshotter.Helpers;

namespace Cloudoman.AwsTools.Snapshotter.Services
{
    

    /// <summary>
    /// Flushes all file system data to disk
    /// </summary>
    public class SyncService
    {
        static readonly string FilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\sync.exe";
        readonly string _drive;

        public SyncService(string drive)
        {
            _drive = drive;
            RunSyncExecutable();
        }

        void RunSyncExecutable()
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = FilePath,
                Arguments = _drive,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            Logger.Info("Flushing writes to disk:" +_drive, "SyncService.RunSyncExecutable");
            try
            {
                var process = Process.Start(processStartInfo);
                process.WaitForExit();
                Logger.Info(process.StandardOutput.ReadToEnd(), "SyncService.RunSyncExecutable");
            }
            catch (Exception e)
            {
                var message = "Could not start Sync.exe: " + e.Message;
                Logger.Error(message,"SyncService.RunSyncExecutable");
            }
        }
    }
}
