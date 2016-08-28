using System;
using System.Text;
using System.Windows.Documents;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public class HexFormatter : FormatterBase
    {

        private StringBuilder _builder = new StringBuilder();


        public HexFormatter(bool showTime, bool showIp) : base(showTime, showIp)
        {

        }

        protected override void OnFormatMessage(Piece msg, Encoding encoding, Paragraph target)
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

            target.Inlines.Add(new Run(_builder.ToString()));       
        }

    }
}
