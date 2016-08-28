using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Formatter;
using TcpUdpTool.Model.Parser;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Item;
using TcpUdpTool.ViewModel.Reusable;
using static TcpUdpTool.Model.UdpClientServerStatusEventArgs;

namespace TcpUdpTool.ViewModel
{
    public class UdpViewModel : ObservableObject
    {

        #region Private members

        private UdpClientServer _udpClientServer;
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

        private HistoryViewModel _historyViewModel = new HistoryViewModel(
            new PlainTextFormatter(true, true), new HexFormatter(true, true));
        public HistoryViewModel History
        {
            get { return _historyViewModel; }
        }

        private bool _isServerStarted;
        public bool IsServerStarted
        {
            get { return _isServerStarted; }
            set
            {
                if(_isServerStarted != value)
                {
                    _isServerStarted = value;
                    OnPropertyChanged(nameof(IsServerStarted));
                }
            }
        }

        private InterfaceItem _selectedInterface;
        public InterfaceItem SelectedInterface
        {
            get { return _selectedInterface; }
            set
            {
                if (_selectedInterface != value)
                {
                    _selectedInterface = value;
                    OnPropertyChanged(nameof(SelectedInterface));
                }
            }
        }

        private int? _listenPort;
        public int? ListenPort
        {
            get { return _listenPort; }
            set
            {
                if(_listenPort != value)
                {
                    _listenPort = value;

                    if(!NetworkUtils.IsValidPort(_listenPort.HasValue ? _listenPort.Value : -1, true))
                    {
                        AddError(nameof(ListenPort), "Port must be between 0 and 65535.");
                    }
                    else
                    {
                        RemoveError(nameof(ListenPort));
                    }

                    OnPropertyChanged(nameof(ListenPort));
                }
            }
        }

        private string _sendIpAddress;
        public string SendIpAddress
        {
            get { return _sendIpAddress; }
            set
            {
                if(_sendIpAddress != value)
                {
                    _sendIpAddress = value;

                    if(String.IsNullOrWhiteSpace(_sendIpAddress))
                    {
                        AddError(nameof(SendIpAddress), "IP address cannot be empty.");
                    }
                    else
                    {
                        RemoveError(nameof(SendIpAddress));
                    }

                    OnPropertyChanged(nameof(SendIpAddress));
                }
            }
        }

        private int? _sendPort;
        public int? SendPort
        {
            get { return _sendPort; }
            set
            {
                if(_sendPort != value)
                {
                    _sendPort = value;

                    if(!NetworkUtils.IsValidPort(_sendPort.HasValue ? _sendPort.Value : -1, false))
                    {
                        AddError(nameof(SendPort), "Port must be between 1 and 65535.");
                    }
                    else
                    {
                        RemoveError(nameof(SendPort));
                    }

                    OnPropertyChanged(nameof(SendPort));
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
                OnPropertyChanged("Message");
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
                    OnPropertyChanged("PlainTextSendTypeSelected");
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
                    OnPropertyChanged("HexSendTypeSelected");
                }
            }
        }

        #endregion

        #region Public commands

        public ICommand StartStopCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (IsServerStarted)
                    {
                        Stop();
                    }
                    else
                    {
                        Start();
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

        public UdpViewModel()
        {
            _udpClientServer = new UdpClientServer();
            _parser = new PlainTextParser();
            LocalInterfaces = new ObservableCollection<InterfaceItem>();

            _udpClientServer.StatusChanged +=
                (sender, arg) =>
                {
                    IsServerStarted = (arg.ServerStatus == EServerStatus.Started);

                    if (IsServerStarted)
                    {
                        History.Header = "Listening on: < " + arg.ServerInfo.ToString() + " >";
                    }
                    else
                    {
                        History.Header = "Conversation History";
                    }
                };

            _udpClientServer.Received +=
                (sender, arg) =>
                {
                    History.Transmissions.Append(arg.Message);
                };

            ListenPort = 0;
            PlainTextSendTypeSelected = true;
            History.Header = "Conversation History";
            Message = "";
            SendIpAddress = "";
            SendPort = 0;

            BuildInterfaceList(Properties.Settings.Default.IPv6Support);

            Properties.Settings.Default.SettingChanging +=
                (sender, e) =>
                {
                    if(e.SettingName == nameof(Properties.Settings.Default.IPv6Support))
                    {
                        BuildInterfaceList((bool)e.NewValue);
                    }                
                };           
        }

        #endregion

        #region Private functions

        private void Start()
        {
            if (!ValidateStart())
                return;

            try
            {
                _udpClientServer.Start(SelectedInterface.Interface, ListenPort.Value);
            }
            catch(Exception ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
            }         
        }

        private void Stop()
        {
            _udpClientServer.Stop();
        }

        private async void Send()
        {
            if (!ValidateSend())
                return;

            try
            {
                var data = _parser.Parse(Message, SettingsUtils.GetEncoding());

                var msg = new Piece(data, Piece.EType.Sent);
                var res = await _udpClientServer.SendAsync(SendIpAddress, SendPort.Value, msg);
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
                Console.Write(ex.StackTrace);
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


        private bool ValidateStart()
        {
            string error = null;
            if (HasError(nameof(ListenPort)))
                error = GetError(nameof(ListenPort));

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
            if (HasError(nameof(SendIpAddress)))
                error = GetError(nameof(SendIpAddress));
            else if (HasError(nameof(SendPort)))
                error = GetError(nameof(SendPort));

            if (error != null)
            {
                DialogUtils.ShowErrorDialog(error);
                return false;
            }

            return true;
        }

        private void BuildInterfaceList(bool ipv6)
        {
            LocalInterfaces.Clear();

            // build interface list
            LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Any, IPAddress.Any));
            if(ipv6) LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Any, IPAddress.IPv6Any));
            foreach (var i in NetworkUtils.GetActiveInterfaces())
            {

                if (i.IPv4Address != null)
                {
                    LocalInterfaces.Add(new InterfaceItem(
                        InterfaceItem.EInterfaceType.Specific, i.IPv4Address));
                }

                if (i.IPv6Address != null && ipv6)
                {
                    LocalInterfaces.Add(new InterfaceItem(
                        InterfaceItem.EInterfaceType.Specific, i.IPv6Address));
                }
            }
        }

        #endregion

    }
}
