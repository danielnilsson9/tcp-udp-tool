using System.Text;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    class PlainTextFormatter : IFormatter
    {

        public void Format(Piece msg, StringBuilder builder, Encoding encoding = null)
        {
            if(encoding == null)
            {
                encoding = Encoding.Default;
            }

            StringBuilder strb = new StringBuilder();

            builder.AppendFormat("[{0}]{1}: ", msg.Timestamp.ToString("HH:mm:ss.fff"), msg.IsSent ? "S" : "R");
            builder.Append(encoding.GetString(msg.Data));
            builder.AppendLine();
        }
    }
}
