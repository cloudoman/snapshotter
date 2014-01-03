using System.Collections.Generic;

namespace Cloudoman.DiskTools.Models
{
    public class Disk
    {
        public int Num { get; set; }
        public string Status { get; set; }
        public string Size { get; set; }
        public string Free { get; set; }
        public string Dyn { get; set; }
        public string Gpt { get; set; }
    }

    public  class DiskComparer : IEqualityComparer<Disk>
    {
        public bool Equals(Disk x, Disk y)
        {
            return x.Num == y.Num &&
                   x.Status == y.Status &&
                   x.Size == y.Size &&
                   x.Free == y.Free &&
                   x.Dyn == y.Dyn &&
                   x.Gpt == y.Gpt;
        }

        public int GetHashCode(Disk obj)
        {
            unchecked
            {
                if (obj == null)
                    return 0;
                int hashCode = obj.Num.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Num.GetHashCode();
                return hashCode;
            }
        }
    }
}
