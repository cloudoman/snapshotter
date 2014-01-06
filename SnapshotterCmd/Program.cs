using System;
using Cloudoman.AwsTools.Snapshotter;
using Cloudoman.AwsTools.Snapshotter.Models;
using Cloudoman.AwsTools.Snapshotter.Services;
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
                    case "snapshotvolumes":

                        var snapRequest = new SnapshotVolumesRequest { 
                            BackupName = parsed.BackupName, 
                            WhatIf = parsed.WhatIf, 
                        };

                        new SnapshotVolumeService(snapRequest).StartBackup();

                        //var backupManager = new BackupManager(backupRequest);
                        //backupManager.StartBackup();
                        break;

                    //case "restore":
                    //    var request = new RestoreRequest { 
                    //        BackupName = parsed.BackupName, 
                    //        TimeStamp = parsed.TimeStamp, 
                    //        ForceDetach = parsed.ForceDetach,
                    //        WhatIf = parsed.WhatIf,
                    //        AttachOnly = parsed.AttachOnly
                    //    };
                    //    var restoreManager = new RestoreManager(request);
                    //    restoreManager.StartRestore();
                    //    break;
                    case "restoresnapshots":
                        var restoreSnapshotsRequest = new RestoreSnapshotsRequest
                        {
                            BackupName = parsed.BackupName,
                            TimeStamp = parsed.TimeStamp,
                            WhatIf = parsed.WhatIf
                        };

                        new RestoreSnapshotsService(restoreSnapshotsRequest).StartRestore();

                        break;
                    case "listsnapshots":
                        //var restoreRequest = new RestoreRequest {
                        //    BackupName = parsed.BackupName, 
                        //    TimeStamp = parsed.TimeStamp,
                        //    AttachOnly = parsed.AttachOnly
                        //};

                        //new RestoreManager(restoreRequest).ListSnapshots();
                        var listSnapshotsRequest = new ListSnapshotsRequest
                        {
                            BackupName = parsed.BackupName,
                            TimeStamp = parsed.TimeStamp
                        };
                        new ListSnapshotsService(listSnapshotsRequest).ListSnapshots();
                        break;

                    case "listvolumes":
                        var restoreRequest2 = new RestoreRequest
                        {
                            BackupName = parsed.BackupName,
                            TimeStamp = parsed.TimeStamp,
                            AttachOnly = parsed.AttachOnly
                        };

                        new RestoreManager(restoreRequest2).ListVolumes();
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
