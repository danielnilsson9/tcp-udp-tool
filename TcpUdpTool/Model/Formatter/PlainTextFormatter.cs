using System.Text;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public class PlainTextFormatter : IFormatter
    {
        private Encoding _encoding;

        public PlainTextFormatter(Encoding encoding = null)
        {
            _encoding = encoding;
            if (_encoding == null)
            {
                _encoding = Encoding.Default;
            }
        }

        public string Format(Transmission msg)
        {          
            return _encoding.GetString(msg.Data);
        }
    }
}
