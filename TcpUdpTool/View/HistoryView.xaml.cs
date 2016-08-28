using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TcpUdpTool.ViewModel.Helper;

namespace TcpUdpTool.View
{
    /// <summary>
    /// Interaction logic for TcpClientView.xaml
    /// </summary>
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            DataContextChanged += (sender, e) =>
            {
                if (e.NewValue is IRichTextboxHelper)
                {
                    HistoryTextBox.Document = ((IRichTextboxHelper)e.NewValue).Document;
                }
            };

            InitializeComponent();


            /*
            var doc = new FlowDocument();

            HistoryTextBox.Document = doc;

            var timeColor = new SolidColorBrush(Color.FromRgb(130, 130, 130));
            var recvColor = new SolidColorBrush(Color.FromRgb(40, 40, 200));
            var sendColor = new SolidColorBrush(Color.FromRgb(200, 40, 40));


            var par = new Paragraph();
           
            var time = new Run("[23:23:45]");
            var ip = new Run("[10.3.45.3:2000]");
            var rec = new Run("R:" + Environment.NewLine);
            var text = new Run("Hello World! This is a first message.");

            var par2 = new Paragraph();
            var time2 = new Run("[10:02:21]");
            var ip2 = new Run("[127.0.0.1:4500]");
            var rec2 = new Run("S:" + Environment.NewLine);
            var text2 = new Run("Hello response.");

            time.Foreground = timeColor;
            ip.Foreground = recvColor;
            rec.Foreground = recvColor;

            time2.Foreground = timeColor;
            ip2.Foreground = sendColor;
            rec2.Foreground = sendColor;

            //par.Inlines.Add(time);
            par.Inlines.Add(ip);
            par.Inlines.Add(rec);
            par.Inlines.Add(text);

            //par2.Inlines.Add(time2);
            par2.Inlines.Add(ip2);
            par2.Inlines.Add(rec2);
            par2.Inlines.Add(text2);

            par.TextAlignment = TextAlignment.Left;
            par2.TextAlignment = TextAlignment.Left;

            doc.Blocks.Add(par);
            doc.Blocks.Add(par2);

           */

            // always scroll to end when text changes.
            HistoryTextBox.TextChanged += 
                (sender, e) =>
                {
                    HistoryTextBox.ScrollToEnd();
                };
        }
    }
}
