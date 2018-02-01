using System.Windows.Input;
using TcpUdpTool.Properties;
using TcpUdpTool.ViewModel.Base;

namespace TcpUdpTool.ViewModel
{
    public class MainViewModel : ObservableObject
    {

        public TcpClientViewModel TcpClientViewModel { get; private set; }

        public TcpServerViewModel TcpServerViewModel { get; private set; }

        public UdpViewModel UdpViewModel { get; private set; }

        public UdpAsmViewModel UdpAsmViewModel { get; private set; }

        public UdpSsmViewModel UdpSsmViewModel { get; private set; }

        public SettingsViewModel SettingsViewModel { get; private set; }

      
        public ICommand CloseCommand
        {
            get { return new DelegateCommand(OnWindowClose); }
        }


        public MainViewModel()
        {
            TcpClientViewModel = new TcpClientViewModel();
            TcpServerViewModel = new TcpServerViewModel();
            UdpViewModel = new UdpViewModel();
            UdpAsmViewModel = new UdpAsmViewModel();
            UdpSsmViewModel = new UdpSsmViewModel();
            SettingsViewModel = new SettingsViewModel();
        }


        private void OnWindowClose()
        {
            Settings.Default.Save();
            TcpClientViewModel?.Dispose();
            TcpServerViewModel?.Dispose();
            UdpViewModel?.Dispose();
            UdpAsmViewModel?.Dispose();
        }

    }
}