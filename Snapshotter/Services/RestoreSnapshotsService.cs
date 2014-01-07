using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.EC2.Model;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;
using Cloudoman.DiskTools;

namespace Cloudoman.AwsTools.Snapshotter.Services
{
    public class RestoreSnapshotsService
    {

        private readonly RestoreSnapshotsRequest _request;
        private readonly string _derivedBackupName;
        private readonly IEnumerable<SnapshotInfo> _snapshotSet;

        public RestoreSnapshotsService(RestoreSnapshotsRequest request)
        {
            // Save Request
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

        public void StartRestore()
        {
            if (_request.WhatIf)
            {
                Logger.Warning("***** Restore will not be done. WhatIf is True *****", "RestoreManager.StartRestore");
            }

            Logger.Info("Restore Started", "RestoreManager.StartRestore");
            Logger.Info("Backup Name:" + _derivedBackupName, "RestoreManager.StartRestore");

            // Output Volume Info header
            Console.WriteLine(new VolumeInfo().FormattedHeader);

            // Create volume then attach
            _snapshotSet.ToList().ForEach(x =>
            {

                // Output volume info being restored
                Console.WriteLine(x.ToString());

                // Restore each snapshot in the set
                // and attach to a Windows drive
                if (!_request.WhatIf)
                {
                    // Create New Volume and Tag it
                    Logger.Info("Restore Volume:" + x.SnapshotId, "RestoreManager.StartRestore");

                    var volumeId = CreateVolume(x);
                    //TagVolume(x, volumeId);
                    Aws.TagVolume(x,volumeId,x.SnapshotId);
                    CreateDrive(x, volumeId);
                }
            });
            
            Logger.Info("Restore Ended", "RestoreManager.StartRestore");
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


                var volume = Aws.Ec2Client.CreateVolume(createVolumeRequest).CreateVolumeResult.Volume;
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
            string status = "";
            for (int i = 1; i <= retry & loop; i++)
            {
                var describeVolumeRequest = new DescribeVolumesRequest { VolumeId = new List<string> { volumeId } };
                var response = Aws.Ec2Client.DescribeVolumes(describeVolumeRequest);
                var newVolume = response.DescribeVolumesResult.Volume.FirstOrDefault();
                status = newVolume.Status;


                loop = status.ToLower() != "available";

                if (loop)
                {
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

        public void CreateDrive(StorageInfo storageInfo, string volumeId)
        {
            var diskNumber = 0;

            Logger.Info("Create Drive:" + storageInfo.Drive, "RestoreManager.CreateDrive");

            // Detach Volumes as appropriate
            ReleaseAwsDevice(storageInfo);

            //Attach new volume
            var newDisk = AttachVolume(storageInfo, volumeId);

            // Online Disk New Disk
            OnlineDrive(newDisk.Num, storageInfo.Drive);


            // Set Delete on termination to TRUE for restored volume
            SetDeleteOnTermination(storageInfo.DeviceName, true);
        }

        void ReleaseAwsDevice(StorageInfo storageInfo)
        {
            var diskNumber = 0;

            // Detach Volumes as appropriate
            var volume = VolumeAtDevice(storageInfo);
            if (volume != null)
            {
                // Find the Windows physical disk number attached to the AWS device (snapshot.DeviceName)
                var mapping = Aws.GetMapping(storageInfo.DeviceName);
                diskNumber = mapping.DiskNumber;

                if (_request.ForceDetach)
                {
                    // Flush all writes to disk
                    new SyncService(storageInfo.Drive);

                    // Offline Disk assocated with required device
                    OfflineDisk(mapping.DiskNumber);
                    DetachVolume(mapping.VolumeId, mapping.Device);
                }
                else
                {
                    var message = "The AWS Device: " + storageInfo.DeviceName +
                                  " is currently attached to another volume on this server. Please detach volume before restore or set ForceDetach to true";

                    Logger.Error(message, "RestoreManager.RestoreVolume");
                    return;
                }
            }
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
            if (volumeNumber == 0)
            {
                Logger.Error("Error determining volume number for new disk's volume", "OnlineDrive");
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

        DiskTools.Models.Disk AttachVolume(StorageInfo storageInfo, string volumeId)
        {
            var diskpart = new DiskPart();
            var disksBefore = diskpart.ListDisk();

            // Attach volume to EC2 Instance
            try
            {
                Logger.Info("Attaching Volume to Instance:" + volumeId + " @ Device:" + storageInfo.DeviceName, "RestoreVolume");
                var attachRequest = new AttachVolumeRequest
                {
                    InstanceId = InstanceInfo.InstanceId,
                    VolumeId = volumeId,
                    Device = storageInfo.DeviceName
                };

                var result = Aws.Ec2Client.AttachVolume(attachRequest).AttachVolumeResult;
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
            for (int i = 1; i <= retry && loop; i++)
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

        string AttachmentStatus(string volumeId)
        {

            var volume = Aws.Ec2Client.DescribeVolumes(new DescribeVolumesRequest { VolumeId = new List<string> { volumeId } })
                                     .DescribeVolumesResult.Volume.FirstOrDefault();

            if (volume == null)
            {
                var message = "Error. " + volumeId + " was not found attached to this instance. Exitting";
                Logger.Error(message, "RestoreManager.AttachmentStatus");
            }

            var status = volume.Status;
            Logger.Info("Volume :" + volumeId + " status:" + status, "IsAttached");
            return status;
        }

        public void WaitAttachmentStatus(string volumeId, string status)
        {
            const int retry = 12;
            const int waitInSeconds = 10;
            string currentStatus = null;

            for (int i = 1; i <= retry; i++)
            {
                currentStatus = AttachmentStatus(volumeId);
                if (currentStatus != status)
                {
                    Logger.Info("Attachment status:" + currentStatus + " ,Sleep " + waitInSeconds + " seconds.", "WaitAttachmentStatus");
                    Thread.Sleep(waitInSeconds * 1000);
                }
                else
                    return;
            }

            var message = "Volume attachment status still: " + currentStatus + ".Exitting.";
            Logger.Error(message, "WaitForAttachment");
        }

        Volume VolumeAtDevice(StorageInfo storeageInfo)
        {
            // Get AWS Device Name from Snapshot's AWS Resource Tag
            var deviceName = storeageInfo.DeviceName;

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

                var response = Aws.Ec2Client.DetachVolume(detachRequest);
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
                Logger.Error("Error taking disk offline.Diskpart Output:\n" + String.Join("\n", response.Output), "RestoreManager.OfflineDisk");
            }
            Logger.Info("Disk was taken offline", "RestoreManager.OfflineDisk");
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

                var response = Aws.Ec2Client.ModifyInstanceAttribute(modifyAttrRequest);
            }
            catch (Amazon.EC2.AmazonEC2Exception ex)
            {
                Logger.Error("Error setting DeleteOnTermination flag for:" + DeviceName, "SetDeleteOnTermination");
                Logger.Error("Exception:" + ex.Message + "\n" + ex.StackTrace, "RestoreVolume");
            }

        }


    }


}
