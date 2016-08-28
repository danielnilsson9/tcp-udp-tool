using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public abstract class FormatterBase : IFormatter
    {
        private readonly SolidColorBrush SendColor = new SolidColorBrush(Color.FromRgb(200, 40, 40));
        private readonly SolidColorBrush RecvColor = new SolidColorBrush(Color.FromRgb(40, 40, 200));
        private readonly SolidColorBrush TimeColor = new SolidColorBrush(Color.FromRgb(120, 120, 120));


        public bool Time { get; set; }
        public bool IP { get; set; }

        public FormatterBase(bool showTime, bool showIp)
        {
            Time = showTime;
            IP = showIp;
        }

        public Paragraph Format(Piece msg, Encoding encoding = null)
        {
            Paragraph par = new Paragraph();

            if(Time)
            {
                var time = new Run(string.Format("[{0}]", msg.Timestamp.ToString("HH:mm:ss")));
                time.Foreground = TimeColor;
                par.Inlines.Add(time);
            }

            if(IP)
            {
                var ip = new Run(string.Format("[{0}]", msg.IsSent ? msg.Destination : msg.Origin));
                ip.Foreground = msg.IsSent ? SendColor : RecvColor;
                par.Inlines.Add(ip);
            }

            var dir = new Run((msg.IsSent ? "S:" : "R:") + Environment.NewLine);
            dir.Foreground = msg.IsSent ? SendColor : RecvColor;
            par.Inlines.Add(dir);

            OnFormatMessage(msg, encoding, par);

            return par;
        }

        abstract protected void OnFormatMessage(Piece msg, Encoding encoding, Paragraph target);

    }
}
