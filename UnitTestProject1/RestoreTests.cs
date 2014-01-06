using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cloudoman.AwsTools.Snapshotter;
using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;
using Amazon.EC2.Model;
using Amazon.EC2;


namespace Cloudoman.AwsTools.Snapshotter.Tests
{
    [TestClass]
    public class RestoreTests
    {
        [TestMethod]
        public void ListSnapshots()
        {
            var request = new RestoreRequest();
            request.BackupName = "web";
            var restoreManager = new RestoreManager(request);
            restoreManager.ListSnapshots();

        }

        [TestMethod]
        public void ModifyInstanceAttribute()
        {
            var modifyAttrRequest = new ModifyInstanceAttributeRequest
            {
                InstanceId = InstanceInfo.InstanceId,
                BlockDeviceMapping = new List<InstanceBlockDeviceMappingParameter>
                {
                    new InstanceBlockDeviceMappingParameter{
                        DeviceName="xvdf",
                        Ebs = new InstanceEbsBlockDeviceParameter{DeleteOnTermination = false,VolumeId="vol-c32e2eea"}
                    }
                }
            };
            var response = Aws.Ec2Client.ModifyInstanceAttribute(modifyAttrRequest);
            Console.WriteLine(response.ResponseMetadata);
        }

        [TestMethod]
        public void AwsDeviceMappings()
        {
            var test = Aws.DeviceMappings;
            if (test == null)
            { Console.WriteLine("yes"); };

            //Console.WriteLine(test.FirstOrDefault().Device);
        }
    }
}
