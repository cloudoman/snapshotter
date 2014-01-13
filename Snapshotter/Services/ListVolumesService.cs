using System;
using System.Collections.Generic;
using System.Linq;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;
using Amazon.EC2.Model;

namespace Cloudoman.AwsTools.Snapshotter.Services
{
    public class ListVolumesService
    {
        private readonly ListVolumesRequest _request;
        public string DerivedBackupName { get; private set; }
        public string DerivedTimeStamp { get; private set; }

        private List<VolumeInfo> _allVolumes;

        public IEnumerable<VolumeInfo> VolumeSet
        {
            get { return GetVolumeSet(); }
        }

        public ListVolumesService(ListVolumesRequest request)
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
            // Find EC2 Snapshots based for given BackupName
            var filters = new List<Filter> {
                new Filter {Name = "tag:BackupName", Value = new List<string> { DerivedBackupName }},
            };

            var request = new DescribeVolumesRequest { Filter = filters };
            var response = Aws.Ec2Client.DescribeVolumes(request).DescribeVolumesResult;
            var volumes = response.Volume;


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
            Logger.Error(message, "ListVolumesService.GetAllVolumes");
        }

        /// <summary>
        /// Lists snapshots matching timestamp passed in via constructor.
        /// List all available snapshots when timestamp was omitted
        /// </summary>
        public void ListVolumes()
        {
            Logger.Info("Listing Volumes", "ListVolumesService.ListVolumes");
            Logger.Info("Backup Name:" + DerivedBackupName, "ListVolumesService.ListVolumes");

            // Output Header
            Console.WriteLine(new VolumeInfo().FormattedHeader);

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

        string GetLatestVolumeTimeStamp()
        {
            var newest = _allVolumes.Max(x => Convert.ToDateTime(x.TimeStamp));

            return _allVolumes
                        .Where(x => Convert.ToDateTime(x.TimeStamp) == newest)
                        .Select(x => x.TimeStamp).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a set of volumes filtered by timestamp.
        /// </summary>
        /// <returns></returns>
        IEnumerable<VolumeInfo> GetVolumeSet()
        {
            var timeStamp = DerivedTimeStamp ?? GetLatestVolumeTimeStamp();
            return _allVolumes
                    .Where(x => Convert.ToDateTime(x.TimeStamp) == Convert.ToDateTime(timeStamp))
                    .OrderByDescending(x => Convert.ToDateTime(x.TimeStamp)).ToList();
        }
    }
}
