using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Microsoft.Win32;

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
            // Accept EULA for Sync
            try
            {
                var regKey = Registry.CurrentUser.CreateSubKey("Software\\Sysinternals\\Sync");
                regKey.SetValue("EulaAccepted", "1", RegistryValueKind.DWord);
            }
            catch (Exception e)
            {
                Logger.Error("Error accepting Sysinternals Sync.exe's EULA:" + e.Message, "SyncService.SyncService");
            }

            _drive = drive;
            
        }

        public void SyncNow()
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = FilePath,
                Arguments = _drive,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            Logger.Info("Flushing writes to disk:" +_drive, "SyncService.SyncNow");
            try
            {
                var process = Process.Start(processStartInfo);
                process.WaitForExit();
                Logger.Info(process.StandardOutput.ReadToEnd(), "SyncService.SyncNow");
            }
            catch (Exception e)
            {
                var message = "Could not start Sync.exe: " + e.Message;
                Logger.Error(message,"SyncService.SyncNow");
            }
        }
    }
}
