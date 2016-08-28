using System;
using System.Text;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public class HexFormatter : IFormatter
    {

        private bool _printIP = false;
        private bool _printTime = false;


        public HexFormatter(bool printIp, bool printTime)
        {
            SetPrintIP(printIp);
            SetPrintTime(printTime);
        }


        public void SetPrintIP(bool printIp)
        {
            _printIP = printIp;
        }

        public void SetPrintTime(bool printTime)
        {
            _printTime = printTime;
        }

        public void Format(Piece msg, StringBuilder builder, Encoding encoding = null)
        {
            if (_printTime)
            {
                builder.AppendFormat("[{0}]", msg.Timestamp.ToString("HH:mm:ss"));
            }

            if (_printIP)
            {
                builder.AppendFormat("[{0}]", msg.IsSent ? msg.Destination : msg.Origin);
            }

            builder.AppendFormat("{0}: ", msg.IsSent ? "S" : "R");
            builder.AppendLine();

            int count = 0;
            foreach(byte b in msg.Data)
            {
                builder.Append(b.ToString("X2"));

                if(++count % 16 == 0)
                {
                    builder.AppendLine();
                }
                else
                {
                    builder.Append(' ');
                }
            }

            builder.AppendLine();
            builder.AppendLine();
        }

    }
}
