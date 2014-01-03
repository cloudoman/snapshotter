using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloudoman.DiskTools.Models
{
    public class DiskDetail
    {
        public Volume Volume { get; set; }
        public string DiskId { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Path { get; set; }
        public string Target { get; set; }
        public string LunId { get; set; }
        public string LocationPath { get; set; }
        public bool ReadOnly { get; set; }
        public bool BootDisk { get; set; }
        public bool PageFileDisk { get; set; }
        public bool HibernationFileDisk { get; set; }
        public bool CrashdumpDisk { get; set; }
        public bool ClusteredDisk { get; set; }

    }
}
