using System;
using System.Diagnostics;

namespace Cloudoman.AwsTools.Snapshotter.Helpers
{
    public static class Logger
    {
        public static void Error(string message, string module)
        {
            WriteEntry(message, "error", module);
            throw new ApplicationException(message);
        }

        public static void Error(Exception ex, string module)
        {

            WriteEntry(ex.Message, "error", module);
        }

        public static void Warning(string message, string module)
        {
            WriteEntry(message, "warning", module);
        }

        public static void Info(string message, string module)
        {
            WriteEntry(message, "info", module);
        }

        private static void WriteEntry(string message, string type, string module)
        {
            var output  = String.Format("{0}\t{1}\t{2}\t{3}",
                                  DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff"),
                                  type,
                                  module,
                                  message);
            if (type != "info") Console.WriteLine(output);
            Trace.WriteLine(output);
        }


    }
}
