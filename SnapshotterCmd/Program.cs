using System;
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
                        break;

                    case "restoresnapshots":
                        var restoreSnapshotsRequest = new RestoreSnapshotsRequest
                        {
                            BackupName = parsed.BackupName,
                            TimeStamp = parsed.TimeStamp,
                            WhatIf = parsed.WhatIf,
                            ForceDetach = parsed.ForceDetach
                        };

                        new RestoreSnapshotsService(restoreSnapshotsRequest).StartRestore();
                        break;

                    case "listsnapshots":
                        var listSnapshotsRequest = new ListSnapshotsRequest
                        {
                            BackupName = parsed.BackupName,
                            TimeStamp = parsed.TimeStamp
                        };
                        new ListSnapshotsService(listSnapshotsRequest).ListSnapshots();
                        break;

                    case "listvolumes":
                        var listVolumesRequest = new ListVolumesRequest
                        {
                            BackupName = parsed.BackupName,
                            TimeStamp = parsed.TimeStamp
                        };

                        new ListVolumesService(listVolumesRequest).ListVolumes();
                        break;

                    case "restoreVolumes":
                        var restoreVolumeRequest = new RestoreVolumesRequest
                        {
                            BackupName = parsed.BackupName,
                            TimeStamp = parsed.TimeStamp,
                            ForceDetach = parsed.ForceDetach,
                            WhatIf = parsed.WhatIf
                        };

                        new RestoreVolumesService(restoreVolumeRequest).StartRestore();
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
