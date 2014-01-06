using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphaleonis.Win32.Vss;
using Amazon.EC2.Model;
using Amazon.Util;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;

namespace Cloudoman.AwsTools.Snapshotter.Services
{
    public class SnapshotVolumeService
    {
        IVssImplementation _vssImplementation;
        IVssBackupComponents _vssBackupComponents;
        string _derivedBackupName;
        List<VolumeInfo> _volumesInfo;

        readonly SnapshotVolumesRequest _request;


        public SnapshotVolumeService(SnapshotVolumesRequest request)
        {
            // save request
            _request = request;

            // Determine BackName used to tag snapshots
            DeriveBackupName();

            // Find volumes to backup
            EnumerateAttachedVolumes();
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
            Logger.Info("Backup name:" + _derivedBackupName, "BackupManager");
        }

        void EnumerateAttachedVolumes()
        {
            // Get attached volumes to me via AWS API
            var volumes = InstanceInfo.Volumes;

            // Generate a timestamp to tag snapshots
            var timeStamp = AWSSDKUtils.FormattedCurrentTimestampRFC822;

            // Generate additional volume metadata to save using resources tags
            // Exclude Root EBS volume from list
            _volumesInfo = volumes.Where(v => v.Attachment[0].Device != "/dev/sda1").Select(x => new VolumeInfo
            {
                VolumeId = x.Attachment[0].VolumeId,
                DeviceName = x.Attachment[0].Device,
                Drive = Aws.DeviceMappings.Where(d => d.VolumeId == x.VolumeId).Select(d => d.Drive).FirstOrDefault(),
                Hostname = InstanceInfo.HostName,
                BackupName = _derivedBackupName,
                TimeStamp = timeStamp
            }).ToList();
        }

        bool CheckBackupPreReqs()
        {
            if (_volumesInfo.Any()) return true;
            Logger.Warning("No EBS volumes excluding boot drive were found for snapshotting.\nExitting.", "CheckBackupPreReqs");
            return false;
        }

        void BackupVolumes()
        {
            Logger.Info("Job Started", "BackupVolumes");

            // Ouptut Headers
            if (_request.WhatIf) {
                Logger.Warning("***** Backup will not be taken. WhatIf is True *****", "BackupVolumes");
            }
            Console.WriteLine(new VolumeInfo().FormattedHeader);

            // Backup Each Volume
            _volumesInfo.ForEach(x =>
            {
                Console.WriteLine(x.ToString());
                if (!_request.WhatIf)
                {
                    StartVssBackup(x.Drive + ":\\");
                    SnapshotVolume(x);
                    AbortVssBackup();
                }
            });

            Logger.Info("Job Ended", "BackupVolumes");

        }

        public void StartBackup()
        {
            // Check pre-requisites before intiating backup
            if (!CheckBackupPreReqs())
            {
                Logger.Warning("Pre-requisites not met, exitting.", "SnapshotBackup");
                return;
            }

            BackupVolumes();
        }

        void StartVssBackup(string driveName)
        {
            // Use Shadow Copy Service to create consistent filesystem snapshot
            _vssImplementation = VssUtils.LoadImplementation();
            _vssBackupComponents = _vssImplementation.CreateVssBackupComponents();
            _vssBackupComponents.InitializeForBackup(null);
            _vssBackupComponents.SetBackupState(false, false, VssBackupType.Full, false);
            _vssBackupComponents.StartSnapshotSet();
            _vssBackupComponents.AddToSnapshotSet(driveName);
            _vssBackupComponents.PrepareForBackup();
            _vssBackupComponents.DoSnapshotSet();
        }

        void AbortVssBackup()
        {
            _vssBackupComponents.AbortBackup();
        }

        void TagResource(string resourceId, VolumeInfo backupVolumeInfo, string namePrefix = "Snapshotter BackupName")
        {
            // Create Tag Request
            var tagRequest = new CreateTagsRequest
            {
                ResourceId = new List<string> { resourceId },
                Tag = new List<Tag>{
                        new Tag {Key = "TimeStamp", Value = backupVolumeInfo.TimeStamp},
                        new Tag {Key = "HostName", Value = backupVolumeInfo.Hostname},
                        new Tag {Key = "VolumeId", Value = backupVolumeInfo.VolumeId},
                        new Tag {Key = "InstanceId", Value = InstanceInfo.InstanceId},
                        new Tag {Key = "DeviceName", Value = backupVolumeInfo.DeviceName},
                        new Tag {Key = "Drive", Value = backupVolumeInfo.Drive},
                        new Tag {Key = "Name", Value = namePrefix + ":" + _derivedBackupName + ", Drive: " + backupVolumeInfo.Drive},
                        new Tag {Key = "BackupName", Value = _derivedBackupName}
                    }
            };

            // Tag Snapshot
            Aws.Ec2Client.CreateTags(tagRequest);
            Logger.Info("HostName " + InstanceInfo.HostName + ":" + InstanceInfo.InstanceId + " Volume Id:" + backupVolumeInfo.VolumeId + " was tagged.", "TagVolume");
        }

        void SnapshotVolume(VolumeInfo backupVolumeInfo)
        {
            try
            {
                // Create Snapshot Request
                var fullDescription = String.Format("DeviceName:{0}, Drive:{1}", backupVolumeInfo.DeviceName, backupVolumeInfo.Drive);
                var request = new CreateSnapshotRequest
                {
                    VolumeId = backupVolumeInfo.VolumeId,
                    Description = fullDescription
                };

                // Create Snapshot
                var response = Aws.Ec2Client.CreateSnapshot(request);
                var snapshotId = response.CreateSnapshotResult.Snapshot.SnapshotId;

                TagResource(snapshotId, backupVolumeInfo);

                Logger.Info("Created Snapshot:" + snapshotId + " for Volume Id:" + backupVolumeInfo.VolumeId, "SnapShotVolume");

            }
            catch (Exception e)
            {
                Logger.Error(e.StackTrace, "BackupManager.SnapshotVolume");
            }
        }



    }
}
