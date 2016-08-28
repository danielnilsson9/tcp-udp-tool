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

            // always scroll to end when text changes.
            HistoryTextBox.TextChanged += 
                (sender, e) =>
                {
                    HistoryTextBox.ScrollToEnd();
                };
        }
    }
}
