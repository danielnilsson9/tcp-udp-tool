using System.Text;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public class PlainTextFormatter : IFormatter
    {

        private bool _printIP = false;
        private bool _printTime = false;


        public PlainTextFormatter(bool printIp, bool printTime)
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
            if(encoding == null)
            {
                encoding = Encoding.Default;
            }

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
            builder.Append(encoding.GetString(msg.Data));
            builder.AppendLine();
            builder.AppendLine();
        }

    }
}
