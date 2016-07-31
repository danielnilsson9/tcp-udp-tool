using System;
using System.Globalization;
using System.Text;

namespace TcpUdpTool.Model.Parser
{
    public class HexParser : IParser
    {
        public byte[] Parse(string text, Encoding encoding = null)
        {
            string[] parts = text.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            byte[] data = new byte[parts.Length];

            for(int i = 0; i < parts.Length; i++)
            {
                try
                {
                    if (parts[i].Length > 2)
                        throw new FormatException();

                    data[i] = (byte)uint.Parse(parts[i], NumberStyles.AllowHexSpecifier);
                }
                catch(FormatException)
                {
                    throw new FormatException("Incorrect sequence, " + parts[i] + " is not a 8-bit hexadecimal number.");
                }               
            }

            return data;
        }

    }
}
