using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class BackupRequest
    {
        public string BackupName { get; set; }
        public bool WhatIf { get; set; }
    }
}
