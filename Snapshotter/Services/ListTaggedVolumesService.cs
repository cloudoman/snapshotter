using System;
using System.Collections.Generic;
using System.Linq;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;


namespace Cloudoman.AwsTools.Snapshotter.Services
{
    public class ListTaggedVolumesService
    {
        private readonly ListTaggedVolumesRequest _request;
        public string DerivedBackupName { get; private set; }
        public string DerivedTimeStamp { get; private set; }

        private List<VolumeInfo> _allVolumes;

        public ListTaggedVolumesService(ListTaggedVolumesRequest request)
        {
            // Save request 
            _request = request;

            // Derive backup name
            DeriveBackupName();

            // Get All existing volumes for given backup name
            GetAllVolumes();

            // Determine TimeStamp
            DerivedTimeStamp = _request.TimeStamp;
            if (String.IsNullOrEmpty(DerivedTimeStamp)) DerivedTimeStamp = null;

        }

        /// <summary>
        /// BackupName is determined in the following order
        /// <para>1. Use backupname from command line</para>
        /// <para>2. If null use EC2 'Name' resource tag of server</para>
        /// <para>3. If null use EC2 instance hostname</para>
        /// </summary>
        /// <returns>BackupName used to tag snapshots</returns>
        void DeriveBackupName()
        {
            DerivedBackupName = _request.BackupName;
            if (String.IsNullOrEmpty(DerivedBackupName)) DerivedBackupName = InstanceInfo.ServerName;
            if (String.IsNullOrEmpty(DerivedBackupName)) DerivedBackupName = InstanceInfo.HostName;
            Logger.Info("Backup name:" + DerivedBackupName, "ListSnapshotsService.DeriveBackupName");
        }

        void GetAllVolumes()
        {
            // Get attached volumes from AWS API
            var volumes = InstanceInfo.Volumes;

            // Generate metadata for attached volumes as VolumeInfo objects
            var volumesInfo = new List<VolumeInfo>();
            volumes.ForEach(x => volumesInfo.Add(new VolumeInfo
            {
                VolumeId = x.VolumeId,
                BackupName = x.Tag.Get("BackupName"),
                DeviceName = x.Tag.Get("DeviceName"),
                Hostname = x.Tag.Get("HostName"),
                Drive = x.Tag.Get("Drive"),
                TimeStamp = x.Tag.Get("TimeStamp"),
            }));

            // Order Descending by date
            volumesInfo = volumesInfo.OrderByDescending(x => Convert.ToDateTime(x.TimeStamp)).ToList();
            _allVolumes = volumesInfo;
            if (_allVolumes.ToList().Count != 0) return;

            var message = "No volumes were found for BackupName:" + DerivedBackupName + " and timestamp: " + DerivedTimeStamp + ".Exitting";
            Logger.Error(message, "ListTaggedVolumesService.GetAllVolumes");
        }

        /// <summary>
        /// Lists snapshots matching timestamp passed in via constructor.
        /// List all available snapshots when timestamp was omitted
        /// </summary>
        public void ListTaggedVolumes()
        {
            Logger.Info("Listing Snaphshots", "ListSnapshotsService.ListSnapshots");
            Logger.Info("Backup Name:" + DerivedBackupName, "ListSnapshotsService.ListSnapshots");

            // Output Header
            Console.WriteLine(new SnapshotInfo().FormattedHeader);

            // Output Snapshots
            if (DerivedTimeStamp != null)
                _allVolumes
                    .Where(x => Convert.ToDateTime(x.TimeStamp) == Convert.ToDateTime(DerivedTimeStamp))
                    .OrderByDescending(x => Convert.ToDateTime(x.TimeStamp)).ToList()
                    .ToList().ForEach(Console.WriteLine);
            else
                _allVolumes
                    .OrderByDescending(x => Convert.ToDateTime(x.TimeStamp)).ToList()
                    .ToList().ForEach(Console.WriteLine);
        }
    }
}
