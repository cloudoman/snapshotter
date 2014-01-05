﻿using PowerArgs;

namespace Cloudoman.AwsTools.SnapshotterCmd.Powerargs
{

    public class MyArgs
    {
        [ArgRequired]
        [ArgDescription("Operation is either 'backup', 'restore', 'listsnapshots' or 'listvolumes'")]
        public Operation Operation { get; set; }

        [ArgDescription("A name for your backup. Defaults to this EC2 instance's 'name' tag or hostname if unspecified.This name is an AWS resource tag used to either tag or find your snapshots")]
        public string BackupName { get; set; }

        [ArgDescription("The GMT Timestamp of the snapshots you want to restore. Specified optionally.\nFor e.g. \"Fri, 27 Dec 2013 01:51:53 GMT\". Default is to use latest snapshot. You can query for the timestamps of your existing snapshots using the list operation. ")]
        public string TimeStamp { get; set; }

        [ArgDescription("Force detach already attached volumes during restore. Default is '0' or 'False'. Use '1' or 'True' for testing.")]
        public bool ForceDetach { get; set; }

        [ArgDescription("Show what would happen if backup or restore was run. Default is '0' or 'False'. Use '1' or 'True' for testing.")]
        public bool WhatIf { get; set; }

        [ArgDescription("Backup operation only tags attached volumes with metadata. Does not create snapshots")]
        public bool TagOnly { get; set; }

        [ArgDescription("Restore operation only attaches existing volumes if any. Does not create volume from snapshots")]
        public bool AttachOnly { get; set; }

    }

}
