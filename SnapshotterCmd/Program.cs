using System;
using Cloudoman.AwsTools.Snapshotter;
using Cloudoman.AwsTools.Snapshotter.Models;
using Cloudoman.AwsTools.SnapshotterCmd.Powerargs;
using PowerArgs;


namespace Cloudoman.AwsTools.SnapshotterCmd
{
    class Program
    {
        static void Main(string[] args)
        {
            

            // Create Snapshots
            try
            {
                // Get arguments if any
                var parsed = Args.Parse<MyArgs>(args);
                var operation = parsed.Operation.ToString().ToLower();
                var backupName = parsed.BackupName;
                var timeStamp = parsed.TimeStamp;
                var forceDetach = parsed.ForceDetach;
                var whatIf = parsed.WhatIf;


                // Run requested operation
                switch (operation)
                {
                    case "backup":
                        var backupRequest = new BackupRequest { BackupName = backupName, WhatIf = whatIf };
                        var backupManager = new BackupManager(backupRequest);
                        backupManager.StartBackup();
                        break;
                    case "restore":
                        var request = new RestoreRequest { 
                            BackupName = backupName, 
                            TimeStamp = timeStamp, 
                            ForceDetach = forceDetach,
                            WhatIf = whatIf 
                        };
                        var restoreManager = new RestoreManager(request);
                        restoreManager.StartRestore();
                        break;

                    case "list":
                        var restoreRequest = new RestoreRequest { BackupName = backupName, TimeStamp = timeStamp };
                        new RestoreManager(restoreRequest).List();
                        break;

                }

            }
            catch (Exception ex)
            {
                if (ex is UnexpectedArgException || ex is MissingArgException)
                    ArgUsage.GetStyledUsage<MyArgs>().Write();
                else
                    Logger.Error(ex.ToString(), "main");
            }


        }
    }
}
