﻿namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class SnapshotVolumesRequest
    {
        public string BackupName { get; set; }
        public bool WhatIf { get; set; }
    }
}
