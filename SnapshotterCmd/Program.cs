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


                // Run requested operation
                switch (operation)
                {
                    case "backup":
                        var backupRequest = new BackupRequest { 
                            BackupName = parsed.BackupName, 
                            WhatIf = parsed.WhatIf, 
                            TagOnly = parsed.TagOnly 
                        };

                        var backupManager = new BackupManager(backupRequest);
                        backupManager.StartBackup();
                        break;
                    case "restore":
                        var request = new RestoreRequest { 
                            BackupName = parsed.BackupName, 
                            TimeStamp = parsed.TimeStamp, 
                            ForceDetach = parsed.ForceDetach,
                            WhatIf = parsed.WhatIf 
                        };
                        var restoreManager = new RestoreManager(request);
                        restoreManager.StartRestore();
                        break;

                    case "list":
                        var restoreRequest = new RestoreRequest {
                            BackupName = parsed.BackupName, 
                            TimeStamp = parsed.TimeStamp 
                        };

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
