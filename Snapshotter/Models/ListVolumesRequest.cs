namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class ListVolumesRequest
    {
        public string BackupName { get; set; }
        public string TimeStamp { get; set; }      
    }
}