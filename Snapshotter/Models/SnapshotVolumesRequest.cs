using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class SnapshotVolumesRequest
    {
        public string BackupName { get; set; }
        public bool WhatIf { get; set; }
    }

    public class TagVolumesRequest
    {
        public string BackupName { get; set; }
        public bool WhatIf { get; set; }
    }

    public class RestoreSnapshotsRequest
    {
        public string BackupName { get; set; }
        public string TimeStamp { get; set; }
        public bool WhatIf { get; set; }
        public bool ForceDetach { get; set; }
    }

    public class RestoreTaggedVolumesRequest
    {
        public string BackupName { get; set; }
        public string TimeStamp { get; set; }
        public bool WhatIf { get; set; }
        public bool ForceDetach { get; set; }        
    }

    public class ListTaggedVolumesRequest
    {
        public string BackupName { get; set; }        
    }

    public class ListSnapshotsRequest
    {
        public string BackupName { get; set; }
    }
}
