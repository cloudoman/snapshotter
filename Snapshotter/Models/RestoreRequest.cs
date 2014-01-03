using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class RestoreRequest
    {
        public string TimeStamp { get; set; }
        public string BackupName { get; set; }
        public bool WhatIf { get; set; }
        public bool ForceDetach { get; set; }
    }
}
