namespace Cloudoman.AwsTools.Snapshotter.Models
{
    public class ListSnapshotsRequest
    {
        public string BackupName { get; set; }
        public string TimeStamp { get; set; }
    }
}