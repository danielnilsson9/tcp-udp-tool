using System.Text;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public class HexFormatter : IFormatter
    {
        private StringBuilder _builder = new StringBuilder();

        public string Format(Transmission msg)
        {
            _builder.Clear();

            int count = 0;
            foreach (byte b in msg.Data)
            {
                _builder.Append(b.ToString("X2"));

                if (++count % 16 == 0)
                {
                    _builder.AppendLine();
                }
                else
                {
                    _builder.Append(' ');
                }
            }

            return _builder.ToString();
        }
    }
}
