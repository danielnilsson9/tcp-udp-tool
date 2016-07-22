using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    class PlainTextFormatter : IFormatter
    {

        public void Format(Piece msg, StringBuilder builder)
        {
            StringBuilder strb = new StringBuilder();

            builder.AppendFormat("[{0}]{1}: ", msg.Timestamp.ToString("HH:mm:ss.fff"), msg.IsSent ? "S" : "R");
            builder.Append(Encoding.UTF8.GetString(msg.Data));
        }
    }
}
