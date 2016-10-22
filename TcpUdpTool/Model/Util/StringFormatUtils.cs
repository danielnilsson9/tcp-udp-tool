using System;
namespace TcpUdpTool.Model.Util
{
    public class StringFormatUtils
    {

        public static string GetSizeAsString(ulong bytes)
        {
            ulong B = 0, KB = 1024, MB = KB * 1024, GB = MB * 1024, TB = GB * 1024;
            double size = bytes;
            string suffix = nameof(B);

            if (bytes >= TB)
            {
                size = Math.Round((double)bytes / TB, 2);
                suffix = nameof(TB);
            }
            else if (bytes >= GB)
            {
                size = Math.Round((double)bytes / GB, 2);
                suffix = nameof(GB);
            }
            else if (bytes >= MB)
            {
                size = Math.Round((double)bytes / MB, 2);
                suffix = nameof(MB);
            }
            else if (bytes >= KB)
            {
                size = Math.Round((double)bytes / KB, 2);
                suffix = nameof(KB);
            }

            return $"{size} {suffix}";
        }

        public static string GetRateAsString(ulong bitsPerSecond)
        {
            ulong b = 0, Kb = 1000, Mb = Kb * 1000, Gb = Mb * 1000, Tb = Gb * 1000;
            double rate = bitsPerSecond;
            string suffix = nameof(b);

            if (bitsPerSecond >= Tb)
            {
                rate = Math.Round((double)bitsPerSecond / Tb, 2);
                suffix = nameof(Tb);
            }
            else if (bitsPerSecond >= Gb)
            {
                rate = Math.Round((double)bitsPerSecond / Gb, 2);
                suffix = nameof(Gb);
            }
            else if (bitsPerSecond >= Mb)
            {
                rate = Math.Round((double)bitsPerSecond / Mb, 2);
                suffix = nameof(Mb);
            }
            else if (bitsPerSecond >= Kb)
            {
                rate = Math.Round((double)bitsPerSecond / Kb, 2);
                suffix = nameof(Kb);
            }

            return $"{rate} {suffix}ps";
        }



    }
}
