namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class TagVolumesRequest
    {
        public string BackupName { get; set; }
        public bool WhatIf { get; set; }
    }
}