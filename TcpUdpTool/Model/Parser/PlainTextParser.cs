using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Parser
{
    public class PlainTextParser : IParser
    {
        private Encoding _encoding;
        private object _lock = new object();

        public PlainTextParser(Encoding encoding)
        {
            SetEncoding(encoding);
        }

        public void SetEncoding(Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding cannot be null.");
            }

            // Do not allow change of encoding in the middle
            // of a parse operation.
            lock (_lock)
            {
                _encoding = encoding;
            }           
        }

        public byte[] Parse(string text)
        {
            List<byte> res = new List<byte>(text.Length * 2);
            StringBuilder cache = new StringBuilder();
            
            bool escape = false;
            bool escapeHex = false;
            bool escapeUnicode = false;
            string hexStr = "";

            lock(_lock)
            {
                foreach (char c in text)
                {
                    if (escape)
                    {
                        if (c == '\\')
                            cache.Append('\\');
                        else if (c == '0')
                            cache.Append('\0');
                        else if (c == 'a')
                            cache.Append('\a');
                        else if (c == 'b')
                            cache.Append('\b');
                        else if (c == 'f')
                            cache.Append('\f');
                        else if (c == 'n')
                            cache.Append('\n');
                        else if (c == 'r')
                            cache.Append('\r');
                        else if (c == 't')
                            cache.Append('\t');
                        else if (c == 'v')
                            cache.Append('\v');
                        else if (c == 'x')
                            escapeHex = true;
                        else if (c == 'u')
                            escapeUnicode = true;
                        else
                            throw new FormatException("Incorrect escape sequence found, \\"
                                + c + " is not allowed.");

                        escape = false;
                    }
                    else if (escapeHex)
                    {
                        hexStr += c;

                        if (hexStr.Length == 2)
                        {
                            try
                            {
                                // adding binary data that should not be character encoded,
                                // encode and move previous data to result and clear cache.
                                res.AddRange(_encoding.GetBytes(cache.ToString()));
                                cache.Clear();
                                res.Add((byte)int.Parse(hexStr, NumberStyles.AllowHexSpecifier));
                            }
                            catch (FormatException)
                            {
                                throw new FormatException("Incorrect escape sequence found, \\x"
                                    + hexStr + " is not a 8-bit hexadecimal number.");
                            }

                            escapeHex = false;
                            hexStr = "";
                        }
                    }
                    else if (escapeUnicode)
                    {
                        hexStr += c;

                        if (hexStr.Length == 4)
                        {
                            try
                            {
                                cache.Append(Convert.ToChar(int.Parse(hexStr, NumberStyles.AllowHexSpecifier)));
                            }
                            catch (FormatException)
                            {
                                throw new FormatException("Incorrect escape sequence found, \\u"
                                   + hexStr + " is not a 16-bit hexadecimal unicode character code.");
                            }

                            escapeUnicode = false;
                            hexStr = "";
                        }
                    }
                    else
                    {
                        if (c == '\\')
                        {
                            // next char is an escape sequence.
                            escape = true;
                        }
                        else
                        {
                            cache.Append(c);
                        }
                    }
                }

                res.AddRange(_encoding.GetBytes(cache.ToString()));
            }
         
            return res.ToArray();
        }

    }

}
