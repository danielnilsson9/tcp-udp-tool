using System.Text;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public class HexFormatter : IFormatter
    {

        public void Format(Piece msg, StringBuilder builder, Encoding encoding = null)
        {
            builder.AppendFormat("[{0}]{1}: ", msg.Timestamp.ToString("HH:mm:ss"), msg.IsSent ? "S" : "R");
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
