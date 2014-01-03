using System.Collections.Generic;
using System.Linq;
using Amazon.EC2.Model;

namespace Cloudoman.AwsTools.Snapshotter
{
    public static class Extensions
    {

        /// <summary>
        /// Returns the value of the tag with maching key
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="key">Name of the Tag's Key For e.g "Date"</param>
        /// <returns></returns>
        public static string Get(this List<Tag> tags, string key)
        {
            return tags.Where(t => t.Key == key).Select(d => d.Value).FirstOrDefault();
        }

        public static bool GetBool(this string[] rawOutput, string key)
        {
            var firstOrDefault = rawOutput.FirstOrDefault(x => x.ToLower().Contains(key.ToLower()));
            if (firstOrDefault != null)
            {
                var info = firstOrDefault;
                info =  info.Split(':')[1].Trim();
                return info == "Yes";
            }
            return false;
        }

        public static string GetString(this string[] rawOutput, string key)
        {
            var firstOrDefault = rawOutput.FirstOrDefault(x => x.ToLower().Contains(key.ToLower()));
            if (firstOrDefault != null)
            {
                var info = firstOrDefault;
                info = info.Split(':')[1].Trim();
                return info;
            }
            return "null";
        }
    }
}
