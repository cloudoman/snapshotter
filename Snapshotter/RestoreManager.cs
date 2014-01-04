using System.Collections.Generic;
using Amazon.EC2.Model;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;
using System;
using System.Linq;
using System.Threading;
using Cloudoman.DiskTools;


namespace Cloudoman.AwsTools.Snapshotter
{
    public class RestoreManager
    {
        private readonly string _backupName;
        private readonly string _timeStamp;
        private readonly IEnumerable<SnapshotInfo> _allSnapshots;
        private readonly RestoreRequest _request;

        public RestoreManager(RestoreRequest request)
        {
            _request = request;

            // Get Backup Name from Request or from Instance NAME tag
            _backupName = _request.BackupName ?? InstanceInfo.ServerName;
            if (_backupName == null)
            {
                var message = "When a backupname is not provided, it's defauted to this EC2 Instances's 'Name' tag.";
                message += "Unable to determing either. Falling back to this EC2 Instance's hostname.";

                Logger.Info(message, "RestoreManager");
            }


            // Get All Snapshots with given backup name
            _allSnapshots = GetAllSnapshots();
            if (_allSnapshots.ToList().Count == 0 )
            {
                var message = "No snapshots were found for BackupName:" + _backupName + " and timestamp: " + _timeStamp + ".Exitting"; 
                Logger.Info(message, "RestoreManager");
                return;
            }

            // Get timestamp from Request or default to latest in _snapshotInfo
            _timeStamp = _request.TimeStamp;
            if (String.IsNullOrEmpty(_timeStamp)) _timeStamp = null;
            if (_timeStamp == null && GetLatestSnapshotTimeStamp() == null)
            {
                var message = "No timestamp was explicitly provided. Unable to determine the timestamp of the latest snapshot. Exitting.";
                Logger.Error(message, "RestoreManager");
            }

        }


        IEnumerable<SnapshotInfo> GetAllSnapshots()
        {

            // Find EC2 Snapshots based for given BackupName
            var filters = new List<Filter> {
                new Filter {Name = "tag:BackupName", Value = new List<string> { _backupName }},
            };

            var request = new DescribeSnapshotsRequest { Filter = filters };
            var snapshots = InstanceInfo.Ec2Client.DescribeSnapshots(request).DescribeSnapshotsResult.Snapshot;

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
            return snapshotsInfo;
        }

        string GetLatestSnapshotTimeStamp()
        {
            var newest = _allSnapshots.Max(x => Convert.ToDateTime(x.TimeStamp));

            var something = _allSnapshots
                        .Where(x => Convert.ToDateTime(x.TimeStamp) == newest)
                        .Select(x => x.TimeStamp).FirstOrDefault();
            return something;
        }

        string GetOldestSnapshotTimeStamp()
        {
            var oldest = _allSnapshots.Min(x => Convert.ToDateTime(x.TimeStamp));
            return _allSnapshots
                        .Where(x => Convert.ToDateTime(x.TimeStamp) == oldest)
                        .Select(x => x.TimeStamp).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a set of snapshots filtered by BackupName for a given timestamp.
        /// The timestamp is passed via the construcstor or defaulted to latest timestamp.
        /// </summary>
        /// <returns></returns>
        public List<SnapshotInfo> GetSnapshotSet()
        {
            // Filter list by timestamp and order ascending
            var timeStamp = _timeStamp ?? GetLatestSnapshotTimeStamp();
            return _allSnapshots
                    .Where(x => Convert.ToDateTime(x.TimeStamp) == Convert.ToDateTime(timeStamp))
                    .OrderByDescending(x => Convert.ToDateTime(x.TimeStamp)).ToList();
        }

        public void StartRestore()
        {
            if (_request.WhatIf)
            {
                Logger.Warning("***** Restore will not be done. WhatIf is True *****", "RestoreManager.StartRestore");
            }

            Logger.Info("Restore Started", "RestoreManager.StartRestore");
            Logger.Info("Backup Name:" + _backupName, "RestoreManager.StartRestore");

            // Output Volume Info header
            Console.WriteLine(new VolumeInfo().FormattedHeader);

            // Find Snapshots to Restore based on timestamp
            var snapshots = GetSnapshotSet();
            snapshots.ToList().ForEach(x =>
            {
                // Output volume info being restored
                Console.WriteLine(x.ToString());

                // Restore each snapshot in the sanpshot set
                if (!_request.WhatIf) RestoreVolume(x);
            });

            Logger.Info("Restore Ended", "RestoreManager.StartRestore");
        }

        public void RestoreVolume(SnapshotInfo snapshot)
        {
            var diskNumber=0;

            Logger.Info("Restore Volume:" + snapshot.SnapshotId, "RestoreManager.RestoreVolume");

            // Create New Volume and Tag it
            var volumeId = CreateVolume(snapshot);
            TagVolume(snapshot, volumeId);

            // Detach Volumes as appropriate
            var volume = VolumeAtDevice(snapshot);
            if (volume != null)
            {
                // Find the Windows physical disk number attached to the AWS device (snapshot.DeviceName)
                var mapping = AwsDevices.GetMapping(snapshot.DeviceName);
                diskNumber = mapping.DiskNumber;

                if (_request.ForceDetach)
                {
                    // Offline Disk assocated with required device
                    OfflineDisk(mapping.DiskNumber);                    
                    DetachVolume(mapping.VolumeId, mapping.Device);
                }
                else
                {
                    var message = "The AWS Device: " + snapshot.DeviceName +
                                  " is currently attached to another volume on this server. Please detach volume before restore or set ForceDetach to true";

                    Logger.Error(message, "RestoreManager.RestoreVolume");
                    return;
                }
            }
            //Attach new volume
            var newDisk = AttachVolume(snapshot, volumeId);
 
            // Online Disk New Disk
            OnlineDrive(newDisk.Num, snapshot.Drive);


            // Set Delete on termination to TRUE for restored volume
            SetDeleteOnTermination(snapshot.DeviceName, true);
        }


        /// <summary>
        /// Lists snapshots matching timestamp passed in via constructor.
        /// List all available snapshots when timestamp was omitted
        /// </summary>
        public void List()
        {
            Logger.Info("Listing Snaphshots", "RestoreManager.List");
            Logger.Info("Backup Name:" + _backupName, "RestoreManager.List");
            Console.WriteLine(new SnapshotInfo().FormattedHeader);
            if (_timeStamp == null)
                _allSnapshots.ToList().ForEach(Console.WriteLine);
            else
                GetSnapshotSet().ToList().ForEach(Console.WriteLine);
        }

        void SetDeleteOnTermination(string DeviceName, bool deleteOnTermination)
        {
            Logger.Info("SetDeleteOnTermination " + DeviceName + " to " + deleteOnTermination, "SetDeleteOnTermination");

            try
            {
                var modifyAttrRequest = new ModifyInstanceAttributeRequest
                {
                    InstanceId = InstanceInfo.InstanceId,
                    BlockDeviceMapping = new List<InstanceBlockDeviceMappingParameter>
                    {
                        new InstanceBlockDeviceMappingParameter{
                            DeviceName="xvdf",
                            Ebs = new InstanceEbsBlockDeviceParameter{DeleteOnTermination = true,VolumeId="vol-c32e2eea"}
                        }
                    }
                };

                var response = InstanceInfo.Ec2Client.ModifyInstanceAttribute(modifyAttrRequest);
            }
            catch (Amazon.EC2.AmazonEC2Exception ex)
            {
                Logger.Error("Error setting DeleteOnTermination flag for:" + DeviceName, "SetDeleteOnTermination");
                Logger.Error("Exception:" + ex.Message + "\n" + ex.StackTrace, "RestoreVolume");
            }
            
        }

        string CreateVolume(SnapshotInfo snapshot)
        {
            
            Logger.Info("Creating Volume for snapshot :" + snapshot.SnapshotId, "CreateVolume");

            string volumeId = "";
            try
            {
                var createVolumeRequest = new CreateVolumeRequest
                {
                    SnapshotId = snapshot.SnapshotId,
                    AvailabilityZone = InstanceInfo.AvailabilityZone,
                };


                var volume = InstanceInfo.Ec2Client.CreateVolume(createVolumeRequest).CreateVolumeResult.Volume;
                volumeId = volume.VolumeId;
                Logger.Info("Created Volume:" + volumeId, "RestoreVolume");

            }
            catch (Amazon.EC2.AmazonEC2Exception ex)
            {
                Logger.Error("Could not create volume.", "RestoreVolume");
                Logger.Error("Exception:" + ex.Message + "\n" + ex.StackTrace, "RestoreVolume");
                return null;
            }

            // Loop until volume is 'available'
            var retry = 10;
            var waitinSeconds = 30;
            var loop = true;
            string status="";
            for (int i=1; i<=retry & loop; i++)
            {
                var describeVolumeRequest = new DescribeVolumesRequest { VolumeId = new List<string> { volumeId } };
                var response = InstanceInfo.Ec2Client.DescribeVolumes(describeVolumeRequest);
                var newVolume = response.DescribeVolumesResult.Volume.FirstOrDefault();
                status = newVolume.Status;

                
                loop = status.ToLower() != "available";

                if (loop) {
                    Logger.Info("Waiting for volume to become 'available'. Volume status:" + status, "RestoreManager.CreateVolume");
                    Thread.Sleep(waitinSeconds * 1000);
                }

            }

            if (loop)
            {
                Logger.Error("Volume status is still:" + status, "RestoreManager.CreateVolume");
            }

            return volumeId;
        }

        void TagVolume(SnapshotInfo snapshot, string volumeId)
        {
                        // Tag restored volume
            try
            {
                Logger.Info("Tagging restored volume with backup metadata", "RestoreVolume");
                var tagRequest = new CreateTagsRequest
                {
                    ResourceId = new List<string> { volumeId },
                    Tag = new List<Tag>{
                        new Tag {Key = "TimeStamp", Value = snapshot.TimeStamp},
                        new Tag{Key="HostName", Value=InstanceInfo.HostName},
                        new Tag{Key="SnapshotId", Value=snapshot.SnapshotId},
                        new Tag{Key="InstanceID", Value=InstanceInfo.InstanceId},
                        new Tag{Key="DeviceName", Value=snapshot.DeviceName},
                        new Tag{Key="Drive", Value=snapshot.Drive},
                        new Tag{Key="Name", Value="Snapshotter BackupName: " + _backupName + " Drive, " + snapshot.Drive},
                        new Tag{Key="BackupName", Value=_backupName},
                    }
                };

                InstanceInfo.Ec2Client.CreateTags(tagRequest);

            }
            catch (Amazon.EC2.AmazonEC2Exception ex)
            {
                Logger.Error("Error tagging volume:" + volumeId,"RestoreVolume");
                Logger.Error("Exception:" + ex.Message + "\n" + ex.StackTrace, "RestoreVolume");
            }


        }

        Volume VolumeAtDevice(SnapshotInfo snapshot)
        {
            // Get AWS Device Name from Snapshot's AWS Resource Tag
            var deviceName = snapshot.DeviceName;

            // Find volumes attached to device
            var currentVolume = InstanceInfo.Volumes.FirstOrDefault(x => x.Attachment[0].Device == deviceName);

            // Return true if an attached volume was found
            return currentVolume;
        }

        void DetachVolume(string volumeId, string device)
        {


            try
            {
                Logger.Info("Detaching Volume", "RestoreVolume");

                var detachRequest = new DetachVolumeRequest
                {
                    InstanceId = InstanceInfo.InstanceId,
                    VolumeId = volumeId,
                    Device = device,
                    Force = true
                };

                var response = InstanceInfo.Ec2Client.DetachVolume(detachRequest);
                Logger.Info("Attachment Status:" + response.DetachVolumeResult.Attachment.Status, "RestoreVolume.DetachVolume");
                Logger.Info("Detached Volume:" + volumeId + " Device:" + device, "RestoreVolume.DetachVolume");
            }
            catch (Amazon.EC2.AmazonEC2Exception ex)
            {
                Logger.Error("Error while detaching existing volume", "RestoreVolume.DetachVolume");
                Logger.Error("Exception:" + ex.Message + "\n" + ex.StackTrace, "RestoreVolume.DetachVolume");
                return;
            }

            WaitAttachmentStatus(volumeId, status: "available");
        }

        void OfflineDisk(int diskNumber)
        {
            // Offline Disk
            var diskPart = new DiskPart();
            var response = diskPart.OfflineDisk(diskNumber);
            if (!response.Status)
            {
                Logger.Error("Error taking disk offline.Diskpart Output:\n" + String.Join("\n",response.Output), "RestoreManager.OfflineDisk");
            }
            Logger.Info("Disk was taken offline", "RestoreManager.OfflineDisk");
        }

        void OnlineDrive(int diskNumber, string drive)
        {
            Logger.Info("Bringing disk online on device:" + diskNumber, "RestoreManager.OnlineDisk");

            // Online Disk
            var diskPart = new DiskPart();
            var response = diskPart.OnlineDisk(diskNumber);
            if (!response.Status)
            {
                Logger.Error("Error bringing disk online.\n Diskpart Output:" + response.Output, "OnlineDrive");
            }

            Logger.Info("Disk was brought online", "OnlineDrive");

            // Assign Volume appropriate Drive Letter
            var volumeNumber = diskPart.DiskDetail(diskNumber).Volume.Num;
            if (volumeNumber == 0 )
            {
                Logger.Error("Error determining volume number for new disk's volume" , "OnlineDrive");
            }

            Logger.Info("Assigning drive letter", "OnlineDrive");
            var assignResponse = diskPart.AssignDriveLetter(volumeNumber, drive);
            if (assignResponse.Status)
            {
                Logger.Info("Drive Letter successfully assigned", "RestoreManager.OnlineDrive");
                return;
            }

            Logger.Error("Error assigning drive letter.\n Diskpart Output:" + assignResponse.Output, "OnlineDrive");
        }

        DiskTools.Models.Disk AttachVolume(SnapshotInfo snapshot, string volumeId)
        {
            var diskpart = new DiskPart();
            var disksBefore = diskpart.ListDisk();

            // Attach volume to EC2 Instance
            try
            {
                Logger.Info("Attaching Volume to Instance:" + volumeId +" @ Device:" + snapshot.DeviceName, "RestoreVolume");
                var attachRequest = new AttachVolumeRequest
                {
                    InstanceId = InstanceInfo.InstanceId,
                    VolumeId = volumeId,
                    Device = snapshot.DeviceName
                };

                var result = InstanceInfo.Ec2Client.AttachVolume(attachRequest).AttachVolumeResult;
                Logger.Info("Attached Volume:" + volumeId, "RestoreVolume");
                Logger.Info("Attachment result:" + result.Attachment.AttachTime, "RestoreVolume");
            }
            catch (Amazon.EC2.AmazonEC2Exception ex)
            {
                Logger.Error("Error attaching volume.\n Exception:" + ex.Message, "RestoreVolume");
            }

            WaitAttachmentStatus(volumeId, status: "in-use");


            // Loop until new disk is detected by Windows
            var waitInSeconds = 10;
            var retry = 10;
            DiskTools.Models.Disk newDisk = new DiskTools.Models.Disk();

            var loop = true;
            for (int i=1; i<=retry && loop; i++)
            {
                // Wait a few seconds
                Thread.Sleep(waitInSeconds * 1000);
                Logger.Info("Could not detect new disk, sleeping " + waitInSeconds + " seconds", "RestoreManager.AttachVolume");

                loop = diskpart.ListDisk().Count() == disksBefore.Count();
            }
            if (disksBefore.Count() == diskpart.ListDisk().Count())
            {
                Logger.Error("Could not detect new disk after attaching volume", "RestoreManager.AttachVolume");
            }

            var disksAfter = diskpart.ListDisk();
            newDisk = disksAfter.Except(disksBefore, new DiskTools.Models.DiskComparer()).FirstOrDefault();

            if (newDisk.Num == 0)
            {
                Logger.Error("Could not detect new disk number for attached volume", "RestoreManager.AttachVolume");
            }

            Logger.Info("Windows detected new disk:" + newDisk.Num, "RestoreManager.AttachVolume");
            return newDisk;
        }

        string AttachmentStatus (string volumeId)
        {
            
            var volume = InstanceInfo.Ec2Client.DescribeVolumes(new DescribeVolumesRequest { VolumeId = new List<string>{volumeId}})
                                     .DescribeVolumesResult.Volume.FirstOrDefault();

            if (volume == null)
            {
                var message = "Error. " + volumeId + " was not found attached to this instance. Exitting";
                Logger.Error(message, "RestoreManager.AttachmentStatus");                
            }

            var status = volume.Status ;
            Logger.Info("Volume :" + volumeId + " status:" + status,"IsAttached" );
            return status;
        }

        public void WaitAttachmentStatus(string volumeId, string status)
        {
            const int retry = 12;
            const int waitInSeconds = 10;
            string currentStatus = null;

            for (int i = 1; i <= retry; i++ )
            {
                currentStatus = AttachmentStatus(volumeId);
                if (currentStatus != status)
                {
                    Logger.Info("Attachment status:" + currentStatus + " ,Sleep "+ waitInSeconds + " seconds.", "WaitAttachmentStatus");
                    Thread.Sleep(waitInSeconds * 1000);
                }
                else
                    return;
            }
            
            var message ="Volume attachment status still: " + currentStatus + ".Exitting.";
            Logger.Error(message, "WaitForAttachment");
        }

    }
}
