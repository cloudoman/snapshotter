using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.EC2.Model;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;

namespace Cloudoman.AwsTools.Snapshotter.Services
{
    public class ListSnapshotsService
    {
        public string DerivedBackupName { get; private set; }
        public string DerivedTimeStamp { get; private set; }
        private IEnumerable<SnapshotInfo> _allSnapshots;

        public IEnumerable<SnapshotInfo> SnapshotSet
        {
            get { return GetSnapshotSet(); }
        }


        private readonly ListSnapshotsRequest _request;

        public ListSnapshotsService(ListSnapshotsRequest request)
        {
            // Save Request
            _request = request;

            // Determine BackName used to tag snapshots
            DeriveBackupName();

            // Get All exiting snapshots for given backup name
            GetAllSnapshots();

            // Determine TimeStamp
            DeriveTimeStamp();

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

        void DeriveTimeStamp()
        {
            DerivedTimeStamp = _request.TimeStamp;
            if (String.IsNullOrEmpty(DerivedTimeStamp)) DerivedTimeStamp = null;
        }


        /// <summary>
        /// Lists snapshots matching timestamp passed in via constructor.
        /// List all available snapshots when timestamp was omitted
        /// </summary>
        public void ListSnapshots()
        {
            Logger.Info("Listing Snaphshots", "ListSnapshotsService.ListSnapshots");
            Logger.Info("Backup Name:" + DerivedBackupName, "ListSnapshotsService.ListSnapshots");

            // Output Header
            Console.WriteLine(new SnapshotInfo().FormattedHeader);

            // Output Snapshots
            if (DerivedTimeStamp != null)
                _allSnapshots
                    .Where(x => Convert.ToDateTime(x.TimeStamp) == Convert.ToDateTime(DerivedTimeStamp))
                    .OrderByDescending(x => Convert.ToDateTime(x.TimeStamp)).ToList()
                    .ToList().ForEach(Console.WriteLine);
            else
                _allSnapshots
                    .OrderByDescending(x => Convert.ToDateTime(x.TimeStamp)).ToList()
                    .ToList().ForEach(Console.WriteLine);
        }

        void GetAllSnapshots()
        {
            // Find EC2 Snapshots based for given BackupName
            var filters = new List<Filter> {
                new Filter {Name = "tag:BackupName", Value = new List<string> { DerivedBackupName }},
            };

            var request = new DescribeSnapshotsRequest { Filter = filters };
            var snapshots = Aws.Ec2Client.DescribeSnapshots(request).DescribeSnapshotsResult.Snapshot;

            // Generate List<SnapshotInfo> from EC2 Snapshots
            // Get Snapshot meta data from AWS resource Tags
            var snapshotsInfo = new List<SnapshotInfo>();
            snapshots.ForEach(x => snapshotsInfo.Add(new SnapshotInfo
            {
                SnapshotId = x.SnapshotId,
                BackupName = x.Tag.Get("BackupName"),
                DeviceName = x.Tag.Get("DeviceName"),
                Hostname = x.Tag.Get("HostName"),
                Drive = x.Tag.Get("Drive"),
                TimeStamp = x.Tag.Get("TimeStamp"),
            }));

            // Order Descending by date
            snapshotsInfo = snapshotsInfo.OrderByDescending(x => Convert.ToDateTime(x.TimeStamp)).ToList();
            _allSnapshots =  snapshotsInfo;
            if (_allSnapshots.ToList().Count != 0) return;

            var message = "No snapshots were found for BackupName:" + DerivedBackupName + " and timestamp: " + DerivedTimeStamp + ".Exitting";
            Logger.Info(message, "ListSnapshotsService.GetAllSnapshots");
        }

        string GetLatestSnapshotTimeStamp()
        {
            var newest = _allSnapshots.Max(x => Convert.ToDateTime(x.TimeStamp));

            return _allSnapshots
                        .Where(x => Convert.ToDateTime(x.TimeStamp) == newest)
                        .Select(x => x.TimeStamp).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a set of snapshots filtered timestamp.
        /// </summary>
        /// <returns></returns>
        IEnumerable<SnapshotInfo> GetSnapshotSet()
        {
            var timeStamp = DerivedTimeStamp ?? GetLatestSnapshotTimeStamp();
            return _allSnapshots
                    .Where(x => Convert.ToDateTime(x.TimeStamp) == Convert.ToDateTime(timeStamp))
                    .OrderByDescending(x => Convert.ToDateTime(x.TimeStamp)).ToList();
        }

    }
}
