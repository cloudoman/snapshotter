using System.Collections.Generic;
using System.Linq;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;

namespace Cloudoman.AwsTools.Snapshotter.Services
{
    public class RestoreVolumesService
    {
        private readonly RestoreVolumesRequest _request;
        private readonly string _derivedBackupName;
        private readonly string _derivedTimeStamp;

        private readonly IEnumerable<VolumeInfo> _volumeSet;

        public RestoreVolumesService(RestoreVolumesRequest request)
        {
            // Save request
            _request = request;

            // Use ListVolumesService to get BackupName, TimeStamp and Volumes to restore
            var listVolumesReqeust = new ListVolumesRequest()
            {
                BackupName = request.BackupName,
                TimeStamp = request.TimeStamp
            };

            var listVolumes = new ListVolumesService(listVolumesReqeust);
            _derivedBackupName = listVolumes.DerivedBackupName;
            _derivedTimeStamp = listVolumes.DerivedTimeStamp;
            _volumeSet = listVolumes.VolumeSet;

        }

        public void StartRestore()
        {
            // Restore snapshot service has most of the
            // functionlity for restoring a volume.
            // Need to refactor that to a separate class later
            var restoreSnapshotRequest = new RestoreSnapshotsRequest
            {
                BackupName = _derivedBackupName,
                TimeStamp = _derivedTimeStamp,
                WhatIf = _request.WhatIf,
                ForceDetach = _request.ForceDetach
            };

            var restoreSnapshotService = new RestoreSnapshotsService(restoreSnapshotRequest);
            
            Logger.Info("Restore Volume Started","RestoreVolumesService.StartRestore");
            // Create a drive from each volume in volume set
            _volumeSet.ToList().ForEach(x =>
            {
                var storageInfo = new StorageInfo
                {
                    BackupName = x.BackupName,
                    DeviceName = x.DeviceName,
                    Drive = x.Drive,
                    Hostname = x.Hostname,
                    TimeStamp = x.TimeStamp
                };
                
                restoreSnapshotService.CreateDrive(storageInfo, x.VolumeId);  
            });

            Logger.Info("Restore Volume Ended", "RestoreVolumesService.StartRestore");
        }
    }
}
