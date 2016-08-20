using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Parser;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Item;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class TcpServerViewModel : ObservableObject
    {

        #region Private members

        private TcpServer _tcpServer;
        private IParser _parser;

        #endregion

        #region Public propterties

        private ObservableCollection<InterfaceItem> _localInterfaces;
        public ObservableCollection<InterfaceItem> LocalInterfaces
        {
            get { return _localInterfaces; }
            set
            {
                if(_localInterfaces != value)
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


        private bool _isStarted;
        public bool IsStarted
        {
            get { return _isStarted; }
            set
            {
                _isStarted = value;
                OnPropertyChanged(nameof(IsStarted));
            }
        }

        private bool _isClientConnected;
        public bool IsClientConnected
        {
            get { return _isClientConnected; }
            set
            {
                _isClientConnected = value;
                OnPropertyChanged(nameof(IsClientConnected));
            }
        }

        private InterfaceItem _selectedInterface;
        public InterfaceItem SelectedInterface
        {
            get { return _selectedInterface; }
            set
            {
                if(_selectedInterface != value)
                {
                    _selectedInterface = value;
                    OnPropertyChanged(nameof(SelectedInterface));
                }
            }
        }


        private int? _port;
        public int? Port
        {
            get { return _port; }
            set
            {
                if(_port != value)
                {
                    _port = value;

                    if(!NetworkUtils.IsValidPort(_port.HasValue ? _port.Value : -1, true))
                    {
                        AddError(nameof(Port), "Port must be between 0 and 65535.");
                    }
                    else
                    {
                        RemoveError(nameof(Port));
                    }

                    OnPropertyChanged(nameof(Port));
                }
            }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                if(_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }                
            }
        }

        private bool _plainTextSendTypeSelected;
        public bool PlainTextSendTypeSelected
        {
            get { return _plainTextSendTypeSelected; }
            set
            {
                if (_plainTextSendTypeSelected != value)
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
                if (_hexSendTypeSelected != value)
                {
                    _hexSendTypeSelected = value;
                    OnPropertyChanged(nameof(HexSendTypeSelected));
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
                    if(IsStarted)
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

        public ICommand DisconnectCommand
        {
            get { return new DelegateCommand(Disconnect); }
        }

        public ICommand SendTypeChangedCommand
        {
            get { return new DelegateCommand(SendTypeChanged); }
        }

        #endregion

        #region Constructors

        public TcpServerViewModel()
        {
            _tcpServer = new TcpServer();
            _parser = new PlainTextParser();
            LocalInterfaces = new ObservableCollection<InterfaceItem>();

            _tcpServer.StatusChanged +=
                (sender, arg) =>
                {
                    if(arg.Status == TcpServerStatusEventArgs.EServerStatus.Started)
                    {
                        IsStarted = true;
                        History.Header = "Listening on: < " + arg.ServerInfo.ToString() + " >";
                    }
                    else if(arg.Status == TcpServerStatusEventArgs.EServerStatus.Stopped)
                    {
                        History.Header = "Conversation History";
                        IsStarted = false;
                    }
                    else if(arg.Status == TcpServerStatusEventArgs.EServerStatus.ClientConnected)
                    {
                        History.Header = "Connected client: < " + arg.ClientInfo.ToString() + " >";
                        IsClientConnected = true;
                    }
                    else if(arg.Status == TcpServerStatusEventArgs.EServerStatus.ClientDisconnected)
                    {
                        History.Header = "Listening on: < " + arg.ServerInfo.ToString() + " >";
                        IsClientConnected = false;
                    }               
                };

            _tcpServer.Received +=
                (sender, arg) =>
                {
                    History.Transmissions.Append(arg.Message);
                };


            Port = 0;
            PlainTextSendTypeSelected = true;
            History.Header = "Conversation History";
            Message = "";

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
                _tcpServer.Start(SelectedInterface.Interface, Port.Value);
            }
            catch(System.Net.Sockets.SocketException ex)
            {
                String message = ex.Message;
                if(ex.ErrorCode == 10013)
                {
                    message = "Port " + Port + " is already in use, unable to start server.";
                }

                DialogUtils.ShowErrorDialog(message);
            }
            catch(Exception ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
            }
        }

        private void Stop()
        {
            _tcpServer.Stop();
        }

        private async void Send()
        {
            byte[] data = new byte[0];
            try
            {
                data = _parser.Parse(Message, SettingsUtils.GetEncoding());
            }
            catch (FormatException ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
                return;
            }

            try
            {
                Piece msg = new Piece(data, Piece.EType.Sent);

                PieceSendResult res = await _tcpServer.SendAsync(msg);
                if (res != null)
                {
                    msg.Origin = res.From;
                    msg.Destination = res.To;
                    History.Transmissions.Append(msg);
                }

                Message = "";
            }
            catch(Exception ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
            }
        }

        private void Disconnect()
        {
            _tcpServer.Disconnect();
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
            if (HasError(nameof(Port)))
                error = GetError(nameof(Port));


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
