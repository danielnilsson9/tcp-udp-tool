using System.Windows.Input;
using TcpUdpTool.Properties;
using TcpUdpTool.ViewModel.Base;

namespace TcpUdpTool.ViewModel
{
    public class MainViewModel : ObservableObject
    {

        public TcpClientViewModel TcpClientViewModel { get; set; }

        public TcpServerViewModel TcpServerViewModel { get; set; }

        public UdpViewModel UdpViewModel { get; set; }

        public UdpMulticastViewModel UdpMulticastViewModel { get; set; }

        public SettingsViewModel SettingsViewModel { get; set; }

      
        public ICommand CloseCommand
        {
            get { return new DelegateCommand(OnWindowClose); }
        }


        public MainViewModel()
        {
            TcpClientViewModel = new TcpClientViewModel();
            TcpServerViewModel = new TcpServerViewModel();
            UdpViewModel = new UdpViewModel();
            UdpMulticastViewModel = new UdpMulticastViewModel();
            SettingsViewModel = new SettingsViewModel();
        }


        private void OnWindowClose()
        {
            Settings.Default.Save();
            TcpClientViewModel?.Dispose();
            TcpServerViewModel?.Dispose();
            UdpViewModel?.Dispose();
            UdpMulticastViewModel?.Dispose();
        }

    }
}