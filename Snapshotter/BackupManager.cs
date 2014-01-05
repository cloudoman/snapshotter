using System;
using System.Collections.Generic;
using System.Linq;
using Alphaleonis.Win32.Vss;
using Amazon.EC2.Model;
using Amazon.Util;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;
using System.Net;

namespace Cloudoman.AwsTools.Snapshotter
{
    public class BackupManager
    {
        readonly string _derivedBackupName;
        readonly List<VolumeInfo> _volumesInfo;
        IVssImplementation _vssImplementation;
        IVssBackupComponents _vssBackupComponents;
        readonly BackupRequest _request;

        public BackupManager(BackupRequest request)
        {
            // Get Backup Name from Request or from Instance NAME tag
            _request = request;
            _derivedBackupName = _request.BackupName ?? InstanceInfo.ServerName;
            if ( String.IsNullOrEmpty(_derivedBackupName))
            {
                _derivedBackupName = InstanceInfo.HostName;
                var message = "When a backupname is not provided, it's defauted to this EC2 Instances's 'Name' tag .";
                message += "Unable to determe either. Falling back to this EC2 Instance's hostname.";

                Logger.Info(message, "BackupManager");
                Logger.Info("Backup name:" + _derivedBackupName, "BackupManager");
            }


            // Get Volumes attached to local instance and generate
            // additional meta data (DriveName, Hostname, TimeStamp etc)
            // in preparation for snapshotting

            var volumes = InstanceInfo.Volumes;
            var timeStamp = AWSSDKUtils.FormattedCurrentTimestampRFC822;
            _volumesInfo = volumes.Where(v => v.Attachment[0].Device != "/dev/sda1").Select(x => new VolumeInfo
            {
                VolumeId = x.Attachment[0].VolumeId,
                DeviceName = x.Attachment[0].Device,
                Drive = AwsDevices.AwsDeviceMappings.Where(d => d.VolumeId == x.VolumeId).Select(d => d.Drive).FirstOrDefault(),
                Hostname = InstanceInfo.HostName,
                BackupName = _derivedBackupName,
                TimeStamp = timeStamp
            }).ToList();

        }

        public void StartBackup()
        {
            // Check pre-requisites before intiating backup
            if (!CheckBackupPreReqs())
            {
                Logger.Warning("Pre-requisites not met, exitting.", "SnapshotBackup");
                return;
            }

            // Snapshot volumes or Tag Only
            Logger.Info("Job Started", "BackupManager");
            if (_request.TagOnly)
                _volumesInfo.ForEach(x => TagResource(x.VolumeId, x, namePrefix:"Snapshotter Tag:"));
            else 
                BackupVolumes();
            Logger.Info("Job Ended", "BackupManager");
        }

        bool CheckBackupPreReqs()
        {

            // Check instance has EBS volumes to snapshot
            // excluding boot volume
            if (!_volumesInfo.Any())
            {
                Logger.Warning("No EBS volumes excluding boot drive were found for snapshotting.\nExitting.", "CheckBackupPreReqs");
                return false;
            }

            return true;
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
                var response = InstanceInfo.Ec2Client.CreateSnapshot(request);
                var snapshotId = response.CreateSnapshotResult.Snapshot.SnapshotId;

                TagResource(snapshotId, backupVolumeInfo);

                Logger.Info("Created Snapshot:" + snapshotId + " for Volume Id:" + backupVolumeInfo.VolumeId, "SnapShotVolume");

            }
            catch (Exception e)
            {
                Logger.Error(e.StackTrace, "BackupManager.SnapshotVolume");
            }
        }

        void TagResource(string resourceId, VolumeInfo backupVolumeInfo, string namePrefix="Snapshotter BackupName")
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
            InstanceInfo.Ec2Client.CreateTags(tagRequest);
            Logger.Info("HostName " + InstanceInfo.HostName + ":" + InstanceInfo.InstanceId + " Volume Id:" + backupVolumeInfo.VolumeId + " was tagged.", "TagVolume");
        }

        void BackupVolumes()
        {
            
            if (_request.WhatIf)
            {
                Logger.Warning("***** Backup will not be taken. WhatIf is True *****", "BackupVolumes");
            }
            Console.WriteLine(new VolumeInfo().FormattedHeader);
            // Backup Each Volume
            _volumesInfo.ForEach(x =>
            {
                Console.WriteLine(x.ToString());
                if (!_request.WhatIf)
                {
                    // Snapshot Volume
                    var driveName = x.Drive + ":\\";
                    StartVssBackup(driveName);
                    SnapshotVolume(x);
                    AbortVssBackup();
                }
            });
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

        public void GetDrivetoAwsDeviceMapping()
        {
            //_volumesInfo.ForEach(x => {
            //    var something = AwsDevices.GetDriveFromVolumeId(x);
            //    Console.WriteLine(something);
            //});
        }
    }
}
