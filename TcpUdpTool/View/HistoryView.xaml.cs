using System.Windows.Controls;
using System.Linq;
using TcpUdpTool.ViewModel.Helper;
using System.Windows.Controls.Primitives;
using System.IO;
using System;

namespace TcpUdpTool.View
{
    /// <summary>
    /// Interaction logic for TcpClientView.xaml
    /// </summary>
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();

            DataContextChanged += (sender, e) =>
            {
                if (e.NewValue is IContentChangedHelper)
                {
                    ((IContentChangedHelper)e.NewValue).ContentChanged += () =>
                    {
                        if(Properties.Settings.Default.ScrollToEnd)
                        {
                            // containers are generated async, must wait for container to be generated before scrolling.
                            ConversationList.ItemContainerGenerator.StatusChanged += ScrollToEnd;
                        }                     
                    };
                }
            };
        }

        public void ScrollToEnd(object sender, EventArgs arg)
        {
            if (ConversationList.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                var item = ConversationList.Items[ConversationList.Items.Count - 1];
                if (item == null)
                    return;

                ConversationList.ScrollIntoView(item);
                ConversationList.ItemContainerGenerator.StatusChanged -= ScrollToEnd;
            }
        }

    }
}
