using Cloudoman.AwsTools.Snapshotter.Helpers;
using Cloudoman.AwsTools.Snapshotter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloudoman.AwsTools.Snapshotter.Services
{
    public class TagVolumesService
    {
        TagVolumesRequest _request;

        public TagVolumesService(TagVolumesRequest request)
        {
            _request = request;
        }

        public void TagVolumes()
        {
            if (_request.WhatIf)
            {
                Console.WriteLine("*** WhatIf not implemented here yet ***");
            }
            // Most of the logic for tagging currently attached volumes
            // is in the SnapshotVolumesService. Refactor later

            var snapshotVolumesRequest = new SnapshotVolumesRequest
            {
                BackupName = _request.BackupName,
                WhatIf = _request.WhatIf
            };

            var response = new SnapshotVolumeService(snapshotVolumesRequest);
            var volumesInfo  = response.AttachedVolumesInfo;

            var maxtimeStamp = volumesInfo.Max(x => x.AwsTimeStamp);

            volumesInfo.ForEach(x =>
            {
                // SnapshotVolumesService provides current timestamp
                // TagVolumeService needs to use the AWS Volume 'CreateTime'

                var storageInfo= new StorageInfo {
                    BackupName = x.BackupName,
                    DeviceName = x.DeviceName,
                    Drive = x.Drive,
                    Hostname = x.Hostname,
                    TimeStamp = Convert.ToDateTime(maxtimeStamp).ToString("r")
                };

                Aws.TagVolume(storageInfo, x.VolumeId,"This volume was not created from snapshot");
            });

           
        }
    }
}
