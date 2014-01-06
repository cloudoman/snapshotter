using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.EC2.Model;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;

namespace Cloudoman.AwsTools.Snapshotter.Services
{
    public class ListSnapshotsService
    {
        private string _derivedBackupName;
        private string _derivedTimeStamp;
        private IEnumerable<SnapshotInfo> _allSnapshots;
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
            _derivedBackupName = _request.BackupName;
            if (String.IsNullOrEmpty(_derivedBackupName)) _derivedBackupName = InstanceInfo.ServerName;
            if (String.IsNullOrEmpty(_derivedBackupName)) _derivedBackupName = InstanceInfo.HostName;
            Logger.Info("Backup name:" + _derivedBackupName, "ListSnapshotsService.DeriveBackupName");
        }

        /// <summary>
        /// Defaults to oldest time stamp if timestamp not provided
        /// </summary>
        void DeriveTimeStamp()
        {
            _derivedTimeStamp = _request.TimeStamp;
            if (String.IsNullOrEmpty(_derivedTimeStamp)) _derivedTimeStamp = null;
            //if (String.IsNullOrEmpty(_derivedTimeStamp)) _derivedTimeStamp = GetOldestSnapshotTimeStamp();
            //if (_derivedTimeStamp == null)
            //{
            //    var message = "No timestamp was explicitly provided. Unable to determine the timestamp of the oldest snapshot. Exitting.";
            //    Logger.Error(message, "ListSnapshotsService.DeriveTimeStamp");
            //}
        }

        string GetOldestSnapshotTimeStamp()
        {
            var minDatetime = _allSnapshots.Min(x => Convert.ToDateTime(x.TimeStamp));

            return _allSnapshots
                        .Where(x => Convert.ToDateTime(x.TimeStamp) == minDatetime)
                        .Select(x => x.TimeStamp).FirstOrDefault();
        }


        /// <summary>
        /// Lists snapshots matching timestamp passed in via constructor.
        /// List all available snapshots when timestamp was omitted
        /// </summary>
        public void ListSnapshots()
        {
            Logger.Info("Listing Snaphshots", "ListSnapshotsService.ListSnapshots");
            Logger.Info("Backup Name:" + _derivedBackupName, "ListSnapshotsService.ListSnapshots");

            // Output Header
            Console.WriteLine(new SnapshotInfo().FormattedHeader);

            // Output Snapshots
            if (_derivedTimeStamp != null)
                _allSnapshots
                    .Where(x => Convert.ToDateTime(x.TimeStamp) == Convert.ToDateTime(_derivedTimeStamp))
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
                new Filter {Name = "tag:BackupName", Value = new List<string> { _derivedBackupName }},
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

            var message = "No snapshots were found for BackupName:" + _derivedBackupName + " and timestamp: " + _derivedTimeStamp + ".Exitting";
            Logger.Error(message, "ListSnapshotsService.GetAllSnapshots");
        }
    }
}
