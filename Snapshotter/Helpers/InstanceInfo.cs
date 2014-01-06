using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Cloudoman.AwsTools.Snapshotter.Helpers
{
    public class InstanceInfo
    {

        static readonly WebClient Web = new WebClient();

        // Following variables will remain static during operations
        public static readonly string InstanceId;
        public static readonly string Ec2Region;
        public static readonly string ServerName;
        public static readonly string AvailabilityZone;
        public static readonly string HostName = Dns.GetHostName();

        // Volumes and FreeDevices can change during operations
        public static List<Volume> Volumes {get { return GetMyVolumes(); }}
        public static IEnumerable<string> FreeDevices { get { return GetFreeDevices(); } }

        static InstanceInfo()
        {
            InstanceId = GetInstanceId();
            Ec2Region = GetEc2Region();

            var ec2Config = new AmazonEC2Config { ServiceURL = Ec2Region };
            //Aws.Ec2Client = AWSClientFactory.CreateAmazonEC2Client(ec2Config);
            ServerName = GetInstanceTag("Name");
            AvailabilityZone = GetAvailabilityZone();
        }

        static readonly Func<string> GetInstanceId = () => Web.DownloadString("http://169.254.169.254/latest/meta-data/instance-id");

        static readonly Func<string> GetAvailabilityZone = () =>
                Web.DownloadString("http://169.254.169.254/latest/meta-data/placement/availability-zone");

        static readonly Func<string> GetEc2Region = () =>
        {
            var availabilityZone =
                Web.DownloadString("http://169.254.169.254/latest/meta-data/placement/availability-zone");
            return "https://ec2." + availabilityZone.Remove(availabilityZone.Length - 1) + ".amazonaws.com";
        };

        public static readonly Func<string,string> GetInstanceTag = (x) =>
        {
            var filters = new List<Filter>
            {
                new Filter {Name = "resource-type", Value = new List<string> {"instance"}},
                new Filter {Name = "resource-id", Value = new List<string> {InstanceId}},
                new Filter
                {
                    Name = "key",
                    Value = new List<string> {x}
                }
            };
            var tags = Aws.Ec2Client.DescribeTags(new DescribeTagsRequest {Filter = filters}).DescribeTagsResult.ResourceTag;
            return tags.Count == 0 ? null : tags[0].Value;
        };

   
        static List<Volume> GetMyVolumes()
        {
            var request = new DescribeVolumesRequest
            {
                Filter = new List<Filter> {
                    new Filter { Name = "attachment.instance-id",Value = new List<string> { InstanceId }}
                }
            };

            var volumes = Aws.Ec2Client.DescribeVolumes(request).DescribeVolumesResult.Volume;

            if (volumes.Count != 0) return volumes;
            Logger.Info("No attached volumes were found", "GetMyVolumes");
            return null;
        }

        

        static IEnumerable<string> GetFreeDevices()
        {
            // Generate list of all devices (xvdf-xvdp)
            var allDevices = Enumerable.Range('f', 'p' - 'f' + 1).Select(x => "xvd" + (char)x);

            // Find Devices attached to local instance
            // Return Devices not attached to local instance
            var request = new DescribeInstancesRequest {InstanceId = new List<string>{InstanceId}};
            return allDevices.Except(
                    Aws.Ec2Client.DescribeInstances(request)
                        .DescribeInstancesResult
                        .Reservation.First()
                        .RunningInstance.First()
                        .BlockDeviceMapping
                        .Select(x => x.DeviceName)
            );
        }
    }
}
