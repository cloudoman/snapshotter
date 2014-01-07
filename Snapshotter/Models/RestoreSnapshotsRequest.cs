namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class RestoreSnapshotsRequest
    {
        public string BackupName { get; set; }
        public string TimeStamp { get; set; }
        public bool WhatIf { get; set; }
        public bool ForceDetach { get; set; }
    }
}