using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Parser;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Helper;
using TcpUdpTool.ViewModel.Item;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class UdpMulticastViewModel : ObservableObject
    {

        #region Private members

        private UdpMulticastClient _udpClient;
        private IParser _parser;

        #endregion

        #region Public properties

        private ObservableCollection<InterfaceItem> _localInterfaces;
        public ObservableCollection<InterfaceItem> LocalInterfaces
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

        private InterfaceItem _selectedListenInterface;
        public InterfaceItem SelectedListenInterface
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


        private string _sendMulticastGroup;
        public string SendMulticastGroup
        {
            get { return _sendMulticastGroup; }
            set
            {
                if(_sendMulticastGroup != value)
                {
                    _sendMulticastGroup = value;

                    try
                    {
                        var addr = IPAddress.Parse(_sendMulticastGroup);

                        if (!NetworkUtils.IsMulticast(addr))
                        {
                            throw new Exception();
                        }
                        else
                        {
                            RemoveError(nameof(SendMulticastGroup));
                        }
                    }
                    catch (Exception)
                    {
                        if (String.IsNullOrWhiteSpace(_sendMulticastGroup))
                        {
                            AddError(nameof(SendMulticastGroup), "Multicast address cannot be empty.");
                        }
                        else
                        {
                            AddError(nameof(SendMulticastGroup),
                                String.Format("\"{0}\" is not a valid multicast address.", _sendMulticastGroup));
                        }
                    }

                    OnPropertyChanged(nameof(SendMulticastGroup));
                }  
            }
        }

        private int? _sendMulticastPort;
        public int? SendMulticastPort
        {
            get { return _sendMulticastPort; }
            set
            {
                if(_sendMulticastPort != value)
                {
                    _sendMulticastPort = value;

                    if(!NetworkUtils.IsValidPort(_sendMulticastPort.HasValue ? _sendMulticastPort.Value : -1, false))
                    {
                        AddError(nameof(SendMulticastPort), "Port must be between 1 and 65535.");
                    }
                    else
                    {
                        RemoveError(nameof(SendMulticastPort));
                    }

                    OnPropertyChanged(nameof(SendMulticastPort));
                }
            }
        }

        private int _sendTTL;
        public int SendTTL
        {
            get { return _sendTTL; }
            set
            {
                if(_sendTTL != value)
                {
                    _sendTTL = value;
                    
                    if(_sendTTL < 1 || _sendTTL > 255)
                    {
                        AddError(nameof(SendTTL), "TTL must be between 1 and 255.");
                    }
                    else
                    {
                        RemoveError(nameof(SendTTL));
                    }

                    OnPropertyChanged(nameof(SendTTL));
                }
            }
        }

        private InterfaceItem _selectedSendInterface;
        public InterfaceItem SelectedSendInterface
        {
            get { return _selectedSendInterface; }
            set
            {
                if (_selectedSendInterface != value)
                {
                    _selectedSendInterface = value;
                    OnPropertyChanged(nameof(SelectedSendInterface));
                }
            }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        private bool _plainTextSendTypeSelected;
        public bool PlainTextSendTypeSelected
        {
            get { return _plainTextSendTypeSelected; }
            set
            {
                if (value != _plainTextSendTypeSelected)
                {
                    _plainTextSendTypeSelected = value;
                    OnPropertyChanged(nameof(PlainTextSendTypeSelected));
                }
            }
        }

        private bool _hexSendTypeSelected;
        public bool HexSendTypeSelected
        {
            get { return _hexSendTypeSelected; }
            set
            {
                if (value != _hexSendTypeSelected)
                {
                    _hexSendTypeSelected = value;
                    OnPropertyChanged(nameof(HexSendTypeSelected));
                }
            }
        }

        #endregion

        #region Public commands

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

        public ICommand SendCommand
        {
            get { return new DelegateCommand(Send); }
        }

        public ICommand SendTypeChangedCommand
        {
            get { return new DelegateCommand(SendTypeChanged); }
        }

        #endregion

        #region Constructors

        public UdpMulticastViewModel()
        {
            _udpClient = new UdpMulticastClient();
            _parser = new PlainTextParser();
            LocalInterfaces = new ObservableCollection<InterfaceItem>();

            _udpClient.Received +=
                (sender, arg) =>
                {
                    DispatchHelper.Invoke(() => History.Transmissions.Append(arg.Message));
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
                        _historyViewModel.Header = "Conversation History";
                    }
                };


            MulticastGroup = "";
            MulticastPort = 0;
            SendMulticastGroup = "";
            SendMulticastPort = 0;
            SendTTL = 16;
            Message = "";
            PlainTextSendTypeSelected = true;
            _historyViewModel.Header = "Conversation History";

            BuildInterfaceList(Properties.Settings.Default.IPv6Support);

            Properties.Settings.Default.PropertyChanged +=
                (sender, e) =>
                {
                    if(e.PropertyName == nameof(Properties.Settings.Default.IPv6Support))
                    {
                        BuildInterfaceList(Properties.Settings.Default.IPv6Support);
                    }
                };
        }

        #endregion

        #region Private functions

        private void Join()
        {
            if (!ValidateJoin())
                return;

            try
            {
                _udpClient.Join(IPAddress.Parse(MulticastGroup), MulticastPort.Value,
                                ToEMulticastInterface(SelectedListenInterface.Type),
                                SelectedListenInterface.Interface);
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

        private async void Send()
        {
            if (!ValidateSend())
                return;

            try
            {
                var data = _parser.Parse(Message, SettingsUtils.GetEncoding());

                var msg = new Piece(data, Piece.EType.Sent);
                var res = await _udpClient.SendAsync(
                    msg, IPAddress.Parse(SendMulticastGroup),
                    SendMulticastPort.Value, ToEMulticastInterface(SelectedSendInterface.Type),
                    SelectedSendInterface.Interface, SendTTL);

                if (res != null)
                {
                    msg.Origin = res.From;
                    msg.Destination = res.To;
                    History.Transmissions.Append(msg);
                    Message = "";
                }
            }
            catch (Exception ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
            }
        }

        private void SendTypeChanged()
        {
            if (PlainTextSendTypeSelected)
            {
                _parser = new PlainTextParser();
            }
            else
            {
                _parser = new HexParser();
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
            if (HasError(nameof(SendMulticastGroup)))
                error = GetError(nameof(SendMulticastGroup));
            else if (HasError(nameof(SendMulticastPort)))
                error = GetError(nameof(SendMulticastPort));
            else if (HasError(nameof(SendTTL)))
                error = GetError(nameof(SendTTL));

            if (error != null)
            {
                DialogUtils.ShowErrorDialog(error);
                return false;
            }

            return true;
        }

        private UdpMulticastClient.EMulticastInterface ToEMulticastInterface(InterfaceItem.EInterfaceType type)
        {
            UdpMulticastClient.EMulticastInterface res;
            switch (type)
            {
                case InterfaceItem.EInterfaceType.Default:
                    res = UdpMulticastClient.EMulticastInterface.Default;
                    break;
                case InterfaceItem.EInterfaceType.All:
                    res = UdpMulticastClient.EMulticastInterface.All;
                    break;
                default:
                    res = UdpMulticastClient.EMulticastInterface.Specific;
                    break;
            }

            return res;
        }

        private void BuildInterfaceList(bool ipv6)
        {
            LocalInterfaces.Clear();
            // build interface list
            LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Default));
            LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.All));
            foreach (var i in NetworkUtils.GetActiveInterfaces())
            {
                if (i.IPv4Address != null)
                {
                    LocalInterfaces.Add(new InterfaceItem(
                        InterfaceItem.EInterfaceType.Specific, i.IPv4Address));
                }

                if (i.IPv6Address != null && ipv6)
                {
                    LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Specific, i.IPv6Address));
                }
            }

            SelectedListenInterface = LocalInterfaces.FirstOrDefault();
            SelectedSendInterface = LocalInterfaces.FirstOrDefault();
        }

        #endregion

    }
}
