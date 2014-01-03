using System;

namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class VolumeInfo: StorageInfo
    {
        public string VolumeId { get; set; }
        public override string ToString()
        {
            return String.Format("|{0,-30}|{1,-10}|{2,-10}|{3,-10}|{4,-15}|{5,-15}|",
                    TimeStamp, BackupName, DeviceName, Drive, Hostname, VolumeId);
        }

        public string FormattedHeader
        {
            get
            {
                return " " + new String('-', 95) + " \n"
                       + String.Format("|{0,-30}|{1,-10}|{2,-10}|{3,-10}|{4,-15}|{5,-15}|\n",
                           "TimeStamp", "BackupName", "Device", "Drive", "ServerName", "VolumeId")
                       + "|" + new String('-', 95) + "|";
            }
        }
    }
}
