using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloudoman.AwsTools.Snapshotter.Models;

namespace Cloudoman.AwsTools.Snapshotter.Services
{
    public class RestoreTaggedVolumesService
    {
        private readonly RestoreTaggedVolumesRequest _request;
        private readonly string _derivedBackupName;
        private readonly IEnumerable<SnapshotInfo> _snapshotSet;

        public RestoreTaggedVolumesService(RestoreTaggedVolumesRequest request)
        {
            // Save request
            _request = request;

            // Use ListSnapshotsService to get BackupName, TimeStamp and Snapshots
            var listSnapshotsRequest = new ListSnapshotsRequest
            {
                BackupName = request.BackupName,
                TimeStamp = request.TimeStamp
            };

            var listSnapshots = new ListSnapshotsService(listSnapshotsRequest);

            _derivedBackupName = listSnapshots.DerivedBackupName;
            _snapshotSet = listSnapshots.SnapshotSet;

        }
    }
}
