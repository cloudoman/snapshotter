using Amazon;
using Amazon.EC2;
using Cloudoman.AwsTools.Snapshotter.Models;
using System;
using System.Management;
using System.Linq;
using System.Collections.Generic;
using Amazon.EC2.Model;
using Cloudoman.DiskTools;

namespace Cloudoman.AwsTools.Snapshotter.Helpers
{
    public static class Aws
    {
        public static readonly AmazonEC2 Ec2Client;

        static Aws()
        {
            var ec2Config = new AmazonEC2Config { ServiceURL = InstanceInfo.Ec2Region };
            Ec2Client = AWSClientFactory.CreateAmazonEC2Client(ec2Config);
        }

        public static IEnumerable<AwsDeviceMapping> DeviceMappings { get {  return GetAwsDeviceMapping(); }}


        public static AwsDeviceMapping GetMapping(string deviceName)
        {
            var mapping = DeviceMappings.FirstOrDefault(x => x.Device == deviceName);
            if (mapping != null) return mapping;
            var message = "Could not find disk number for device: " + deviceName + ". Exitting.";
            Logger.Error(message, "RestoreManager.OnlineDrive");

            return null;
        }

        static int GetScsiTargetId(string awsDevice)
        {

            // AWS Maps devices to SCSITargetId like this:
            // AWS Device| Location (Windows Disk Property)
            // xvdb | Target ID 1
            // xvdc | Target ID 2
            // xvdd | Target ID 3

            if (awsDevice == "/dev/sda1") return 0;
            var scsiId = awsDevice[awsDevice.Length - 1];

            return (scsiId - 97);
        }

        public static int GetPhysicalDisk(string awsDevice)
        {
            var scsiTargetId = GetScsiTargetId(awsDevice);

            var query = new SelectQuery("Select DeviceId From Win32_DiskDrive where SCSITargetId =" + scsiTargetId);
            var searcher = new ManagementObjectSearcher(query);
            var collection = searcher.Get();

            int disk = 0;
            foreach (var item in collection)
            {
                var deviceId = item["DeviceId"].ToString();

                // The WMI field deviceID normally looks like "\\.\PHYSICALDRIVE2"
                // Extract the disk number only
                disk = Int32.Parse(deviceId.Replace(@"\\.\PHYSICALDRIVE", ""));
            }

            if (disk == 0)
            {
                var message = "Could not find physical disk for AWS Device: " + awsDevice;
                Logger.Error(message, "Aws.GetPhysicalDisk");
                throw new ApplicationException(message);
            }
            return disk;
        }

        /// <summary>
        /// Returns the Windows Physical Disk Number attached to a given AWS Ebs Volume
        /// </summary>
        /// <param name="volume">EBS Volume ID</param>
        /// <returns></returns>
        static int GetDiskFromAwsVolume(Volume volume)
        {
            var device = volume.Attachment[0].Device;

            // AWS is inconsistent. Responds with "/dev/sda1" for root awsDevice
            // but with only "xvdf" or "xvdg" for other devices
            // Prefixing all with  "/dev/" for consistency
            device = device.Contains("/dev/") ? device : "/dev/" + device;

            // Get Windows DiskInfo(DeviceId) from AWSDevice(SCSITargetId) Win32_DiskDrive WMI counter
            var scsiTargetId = GetScsiTargetId(device);
            var query = new SelectQuery("Select DeviceId From Win32_DiskDrive where SCSITargetId ="+ scsiTargetId);
            var searcher = new ManagementObjectSearcher(query);
            var collection = searcher.Get();

            int disk=0;
            foreach (var item in collection)
            {
                var deviceId = item["DeviceId"].ToString();

                // The WMI field deviceID normally looks like "\\.\PHYSICALDRIVE2"
                // Extract the disk number only
                disk = Int32.Parse(deviceId.Replace(@"\\.\PHYSICALDRIVE",""));
            }

            return disk;
        }


        static IEnumerable<AwsDeviceMapping> GetAwsDeviceMapping()
        {
            var volumes = InstanceInfo.Volumes;
            var diskPart = new DiskPart();
            var mappings = new List<AwsDeviceMapping>();

            // Get all offline disks
            var offlineDisks = diskPart.ListDisk().Where(x => x.Status == "Offline"); ;

            if (offlineDisks.Count() > 0 )
            {
                var message = "All disk need to be online in order for mapping an AWS Device to a Windows volume. Please online all disks. Exitting.";
                Logger.Error(message,"AWSDevices.GetAwsDeviceMapping");
            }

            var awsDeviceMappings = volumes.Select(x => new AwsDeviceMapping
            {
                Device = x.Attachment[0].Device,
                VolumeId = x.VolumeId,
                DiskNumber = GetDiskFromAwsVolume(x),
                VolumeNumber = diskPart.DiskDetail(GetDiskFromAwsVolume(x)).Volume.Num,
                Drive = diskPart.DiskDetail(GetDiskFromAwsVolume(x)).Volume.Letter
            });


            return awsDeviceMappings;
        }


        public static void TagVolume(StorageInfo snapshot, string resourceId, string snapshotId, string namePrefix = "Snapshotter BackupName")
        {
            // Tag restored volume
            try
            {
                Logger.Info("Tagging restored resource with backup metadata", "TagVolume");
                var tagRequest = new CreateTagsRequest
                {
                    ResourceId = new List<string> { resourceId },
                    Tag = new List<Tag>{
                        new Tag {Key = "TimeStamp", Value = snapshot.TimeStamp},
                        new Tag{Key="HostName", Value=InstanceInfo.HostName},
                        new Tag{Key="SnapshotId", Value=snapshotId},
                        new Tag{Key="InstanceID", Value=InstanceInfo.InstanceId},
                        new Tag{Key="DeviceName", Value=snapshot.DeviceName},
                        new Tag{Key="Drive", Value=snapshot.Drive},
                        new Tag{Key="Name", Value=namePrefix + ":" + snapshot.BackupName + " Drive, " + snapshot.Drive},
                        new Tag{Key="BackupName", Value=snapshot.BackupName},
                    }
                };

                Ec2Client.CreateTags(tagRequest);

            }
            catch (Amazon.EC2.AmazonEC2Exception ex)
            {
                Logger.Error("Error tagging volume:" + resourceId, "RestoreVolume");
                Logger.Error("Exception:" + ex.Message + "\n" + ex.StackTrace, "RestoreVolume");
            }
        }
 

    }


}
