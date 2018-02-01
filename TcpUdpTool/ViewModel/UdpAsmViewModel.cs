using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Item;
using TcpUdpTool.ViewModel.Base;
using System.Windows;

namespace TcpUdpTool.ViewModel
{
    public class UdpAsmViewModel : ObservableObject, IDisposable
    {

        #region private members

        private UdpMulticastClient _udpClient;

        #endregion

        #region public properties

        private ObservableCollection<InterfaceAddress> _localInterfaces;
        public ObservableCollection<InterfaceAddress> LocalInterfaces
        {
            get { return _localInterfaces; }
            set
            {
                if (_localInterfaces != value)
                {
                    _localInterfaces = value;
                    OnPropertyChanged(nameof(LocalInterfaces));
                }
            }
        }

        private HistoryViewModel _historyViewModel = new HistoryViewModel();
        public HistoryViewModel History
        {
            get { return _historyViewModel; }
        }

        private SendViewModel _sendViewModel = new SendViewModel();
        public SendViewModel Send
        {
            get { return _sendViewModel; }
        }

        private bool _isGroupJoined;
        public bool IsGroupJoined
        {
            get { return _isGroupJoined; }
            set
            {
                _isGroupJoined = value;
                OnPropertyChanged(nameof(IsGroupJoined));
            }
        }

        private string _multicastGroup;
        public string MulticastGroup
        {
            get { return _multicastGroup; }
            set
            {
                if(_multicastGroup != value)
                {
                    _multicastGroup = value;

                    try
                    {
                        var addr = IPAddress.Parse(_multicastGroup);

                        if (!NetworkUtils.IsMulticast(addr))
                        {
                            throw new Exception();
                        }
                        else
                        {
                            RemoveError(nameof(MulticastGroup));
                        }
                    }
                    catch(Exception)
                    {
                        if(String.IsNullOrWhiteSpace(_multicastGroup))
                        {
                            AddError(nameof(MulticastGroup), "Multicast address cannot be empty.");
                        }
                        else
                        {
                            AddError(nameof(MulticastGroup), 
                                String.Format("\"{0}\" is not a valid multicast address.", _multicastGroup));
                        }                      
                    }

                    OnPropertyChanged(nameof(MulticastGroup));
                }
            }
        }

        private int? _multicastPort;
        public int? MulticastPort
        {
            get { return _multicastPort; }
            set
            {
                if(_multicastPort != value)
                {
                    _multicastPort = value;

                    if(!NetworkUtils.IsValidPort(_multicastPort.HasValue ? _multicastPort.Value : -1, false))
                    {
                        AddError(nameof(MulticastPort), "Port must be between 1 and 65535.");
                    }
                    else
                    {
                        RemoveError(nameof(MulticastPort));
                    }

                    OnPropertyChanged(nameof(MulticastPort));
                }  
            }
        }

        private InterfaceAddress _selectedListenInterface;
        public InterfaceAddress SelectedListenInterface
        {
            get { return _selectedListenInterface; }
            set
            {
                if (_selectedListenInterface != value)
                {
                    _selectedListenInterface = value;
                    OnPropertyChanged(nameof(SelectedListenInterface));
                }
            }
        }
        
        #endregion

        #region public commands

        public ICommand JoinLeaveCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (IsGroupJoined)
                    {
                        Leave();
                    }
                    else
                    {
                        Join();
                    }
                });
            }
        }

        #endregion

        #region constructors

        public UdpAsmViewModel()
        {
            _udpClient = new UdpMulticastClient();
            LocalInterfaces = new ObservableCollection<InterfaceAddress>();

            _sendViewModel.SendData += OnSend;
            _udpClient.Received +=
                (sender, arg) =>
                {
                    History.Append(arg.Message);
                };

            _udpClient.StatusChanged += 
                (sender, arg) =>
                {
                    IsGroupJoined = arg.Joined;

                    if(arg.Joined)
                    {
                        _historyViewModel.Header = "Joined: < " + arg.MulticastGroup + " >";
                    }
                    else
                    {
                        _historyViewModel.Header = "Conversation";
                    }
                };


            MulticastGroup = "";
            MulticastPort = 0;
            Send.MulticastGroup = "";
            Send.Port = 0;
            Send.MulticastTtl = 16;
            _historyViewModel.Header = "Conversation";

            RebuildInterfaceList();

            Properties.Settings.Default.PropertyChanged += (sender, e) =>
            {
                if(e.PropertyName == nameof(Properties.Settings.Default.IPv6Support))
                {
                    RebuildInterfaceList();
                }
            };

            NetworkUtils.NetworkInterfaceChange += () =>
            {
                Application.Current.Dispatcher.Invoke(RebuildInterfaceList);
            };
        }

        #endregion

        #region private functions

        private void Join()
        {
            if (!ValidateJoin())
                return;

            try
            {
                _udpClient.JoinASM(IPAddress.Parse(MulticastGroup), MulticastPort.Value,
                                ToEMulticastInterface(SelectedListenInterface.Type),
                                SelectedListenInterface.Address);
            }
            catch(Exception ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
            }          
        }

        private void Leave()
        {
            _udpClient.Leave();
        }

        private async void OnSend(byte[] data)
        {
            if (!ValidateSend())
                return;

            try
            {
                var msg = new Transmission(data, Transmission.EType.Sent);
                History.Append(msg);
                var res = await _udpClient.SendAsync(
                    msg, IPAddress.Parse(Send.MulticastGroup),
                    Send.Port.Value, ToEMulticastInterface(Send.SelectedInterface.Type),
                    Send.SelectedInterface.Address, Send.MulticastTtl);

                if (res != null)
                {
                    msg.Origin = res.From;
                    msg.Destination = res.To;                 
                    Send.Message = "";
                }
            }
            catch (Exception ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
            }
        }

        private bool ValidateJoin()
        {
            string error = null;
            if (HasError(nameof(MulticastGroup)))
                error = GetError(nameof(MulticastGroup));
            else if (HasError(nameof(MulticastPort)))
                error = GetError(nameof(MulticastPort));

            if (error != null)
            {
                DialogUtils.ShowErrorDialog(error);
                return false;
            }

            return true;
        }

        private bool ValidateSend()
        {
            string error = null;
            if (Send.HasError(nameof(Send.MulticastGroup)))
                error = Send.GetError(nameof(Send.MulticastGroup));
            else if (Send.HasError(nameof(Send.Port)))
                error = Send.GetError(nameof(Send.Port));
            else if (Send.HasError(nameof(Send.MulticastTtl)))
                error = Send.GetError(nameof(Send.MulticastTtl));

            if (error != null)
            {
                DialogUtils.ShowErrorDialog(error);
                return false;
            }

            return true;
        }

        private UdpMulticastClient.EMulticastInterface ToEMulticastInterface(InterfaceAddress.EInterfaceType type)
        {
            UdpMulticastClient.EMulticastInterface res;
            switch (type)
            {
                case InterfaceAddress.EInterfaceType.Default:
                    res = UdpMulticastClient.EMulticastInterface.Default;
                    break;
                case InterfaceAddress.EInterfaceType.All:
                    res = UdpMulticastClient.EMulticastInterface.All;
                    break;
                default:
                    res = UdpMulticastClient.EMulticastInterface.Specific;
                    break;
            }

            return res;
        }

        private void RebuildInterfaceList()
        {
            LocalInterfaces.Clear();
            // build interface list
            LocalInterfaces.Add(new InterfaceAddress(InterfaceAddress.EInterfaceType.Default, null));
            LocalInterfaces.Add(new InterfaceAddress(InterfaceAddress.EInterfaceType.All, null));
            foreach (var nic in NetworkUtils.GetActiveInterfaces())
            {
                foreach (var ip in nic.IPv4.Addresses)
                {
                    LocalInterfaces.Add(new InterfaceAddress(
                        InterfaceAddress.EInterfaceType.Specific, nic, ip));
                }

                if (Properties.Settings.Default.IPv6Support)
                {
                    foreach (var ip in nic.IPv6.Addresses)
                    {
                        LocalInterfaces.Add(new InterfaceAddress(
                           InterfaceAddress.EInterfaceType.Specific, nic, ip));
                    }
                }
            }

            SelectedListenInterface = LocalInterfaces.FirstOrDefault();

            Send.Interfaces = LocalInterfaces;
            Send.SelectedInterface = LocalInterfaces.FirstOrDefault();
        }

        public void Dispose()
        {
            _udpClient?.Dispose();
            _historyViewModel?.Dispose();
        }

        #endregion

    }
}
