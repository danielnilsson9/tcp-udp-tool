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


        public PlainTextParser(Encoding encoding = null)
        {
            _encoding = encoding;
            if(_encoding == null)
            {
                _encoding = Encoding.Default;
            }
        }

        public byte[] Parse(string text)
        {
            StringBuilder b = new StringBuilder();

            bool escape = false;
            bool escapeHex = false;
            string hexStr = "";
            foreach(char c in text)
            {
                if(escape)
                {
                    if (c == '\\')
                        b.Append('\\');
                    else if (c == '0')
                        b.Append('\0');
                    else if (c == 'a')
                        b.Append('\a');
                    else if (c == 'b')
                        b.Append('\b');
                    else if (c == 'f')
                        b.Append('\f');
                    else if (c == 'n')
                        b.Append('\n');
                    else if (c == 'r')
                        b.Append('\r');
                    else if (c == 't')
                        b.Append('\t');
                    else if (c == 'v')
                        b.Append('\v');
                    else if (c == 'x')
                        escapeHex = true;
                    else
                        throw new FormatException("Incorrect escape sequence found, \\" 
                            + c + " is not allowed.");
                    
                    escape = false;
                }
                else if(escapeHex)
                {
                    hexStr += c;

                    if(hexStr.Length == 2)
                    {
                        try
                        {
                            b.Append((char)int.Parse(hexStr, NumberStyles.AllowHexSpecifier));
                        }
                        catch(FormatException)
                        {
                            throw new FormatException("Incorrect escape sequence found, \\x" 
                                + hexStr + " is not a hexadecimal number.");
                        }

                        escapeHex = false;
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
                        b.Append(c);
                    }
                }              
            }
            
            return _encoding.GetBytes(b.ToString());
        }

    }

}
