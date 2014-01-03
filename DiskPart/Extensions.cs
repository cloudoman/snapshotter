using System;
using System.Linq;

namespace Cloudoman.DiskTools
{
    public static class Extensions
    {
        public static string NullIfEmpty(this string str)
        {
            return string.IsNullOrEmpty(str) ? "null" : str;
        }

        /// <summary>
        /// Displays output from diskpart which is a string array
        /// </summary>
        /// <param name="str">String Array containing diskpart output</param>

        public static void Dump(this string[] str)
        {
            str.ToList().ForEach(Console.WriteLine);
        }

        /// <summary>
        /// EXtracts key/value pairs from DiskPart detail output
        /// </summary>
        /// <param name="rawOutput">Raw output from a DiskPart Disk Detail or Volume Detail command</param>
        /// <param name="key">Key to extract, for e.g. "Shadow Copy"</param>
        /// <returns></returns>
        public static bool GetBool(this string[] rawOutput, string key)
        {
            var row = rawOutput.FirstOrDefault(x => x.ToLower().Contains(key.ToLower()));
            if (row == null) return false;
            var info = row.Split(':')[1].Trim();
            return info == "Yes";
        }

        /// <summary>
        /// Extracts key/value pairs from DiskPart detail output
        /// </summary>
        /// <param name="rawOutput">Raw output from a DiskPart Disk Detail or Volume Detail command</param>
        /// <param name="key">Key to extract, for e.g. "Shadow Copy"</param>
        /// <returns></returns>
        public static string GetString(this string[] rawOutput, string key)
        {
            var row = rawOutput.FirstOrDefault(x => x.ToLower().Contains(key.ToLower()));
            if (row == null) return "null";
            var info = row.Split(':')[1].Trim();
            return info;
        }

    }
}
