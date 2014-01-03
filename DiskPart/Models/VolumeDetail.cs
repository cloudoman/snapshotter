namespace Cloudoman.DiskTools.Models
{
    public class VolumeDetail
    {
        public Disk Disk { get; set; }
        public bool ReadOnly { get; set; }
        public bool Hidden { get; set; }
        public bool NoDefaultDriveLetter { get; set; }
        public bool ShadowCopy { get; set; }
        public bool Offline { get; set; }
        public bool BitLockerEncrypted { get; set; }
        public bool Installable { get; set; }
        public string VolumeCapacity { get; set; }
        public string VolumeFreeSpace { get; set; }
    }
}
