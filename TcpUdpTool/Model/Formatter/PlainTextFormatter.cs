using System;
using System.Text;
using System.Windows.Documents;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public class PlainTextFormatter : FormatterBase
    {
        public PlainTextFormatter(bool showTime, bool showIp) : base(showTime, showIp)
        {
        }

        protected override void OnFormatMessage(Piece msg, Encoding encoding, Paragraph target)
        {
            if (encoding == null)
            {
                encoding = Encoding.Default;
            }

            target.Inlines.Add(new Run(encoding.GetString(msg.Data)));
        }

    }
}
