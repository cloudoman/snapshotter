namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class AwsDeviceMapping
    {
        /// <summary>
        /// AWS Volume ID. For e.g. vol-3d162283
        /// </summary>
        public string VolumeId { get; set; }

        /// <summary>
        /// AWS Device Name. For .e.g "/dev/xvdf"
        /// </summary>
        public string Device { get; set; }

        /// <summary>
        ///  Windows Physical Disk Number. For e.g. 0 or 1
        /// </summary>
        public int DiskNumber { get; set; }

        /// <summary>
        /// Windows Volume Number. For e.g. 0 or 1
        /// </summary>
        public int VolumeNumber { get; set; }

        /// <summary>
        /// Windows Drive Letter. For e.g. E (for "E:")
        /// </summary>
        public string Drive { get; set; }
    }
}
