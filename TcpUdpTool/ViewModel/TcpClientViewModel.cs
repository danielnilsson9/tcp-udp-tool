using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Parser;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class TcpClientViewModel : ObservableObject
    {

        #region Private Members

        private IParser _parser;
        private TcpClient _tcpClient;

        #endregion

        #region Public Properties

        private HistoryViewModel _historyViewModel = new HistoryViewModel();
        public HistoryViewModel History
        {
            get { return _historyViewModel; }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        private bool _isConnecting;
        public bool IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                _isConnecting = value;
                OnPropertyChanged(nameof(IsConnecting));
            }
        }

        private string _ipAddress;
        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                if(_ipAddress != value)
                {
                    _ipAddress = value;
                        
                    if(String.IsNullOrWhiteSpace(_ipAddress))
                    {
                        AddError(nameof(IpAddress), "IP address cannot be empty.");
                    }
                    else
                    {
                        RemoveError(nameof(IpAddress));
                    }
                      
                    OnPropertyChanged(nameof(IpAddress));
                }
            }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            set
            {
                if(_port != value)
                {
                    _port = value;

                    if(!NetworkUtils.IsValidPort(_port, false))
                    {
                        AddError(nameof(Port), "Port must be between 1 and 65535.");
                    }
                    else
                    {
                        RemoveError(nameof(Port));
                    }
                }

                OnPropertyChanged(nameof(Port));
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
                if(value != _plainTextSendTypeSelected)
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

        #region Public Commands

        public ICommand ConnectDisconnectCommand
        {
            get
            {
                return new DelegateCommand(() =>
                    {
                        if (IsConnected)
                        {
                            Disconnect();
                        }                         
                        else
                        {
                            Connect();
                        }                          
                    }                  
                );
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

        public TcpClientViewModel()
        {
            _tcpClient = new TcpClient();
            _parser = new PlainTextParser();

            _tcpClient.StatusChanged += 
                (sender, arg) => 
                {
                    IsConnected = arg.Status == TcpClientStatusEventArgs.EConnectStatus.Connected;
                    IsConnecting = arg.Status == TcpClientStatusEventArgs.EConnectStatus.Connecting;

                    if(IsConnected)
                    {
                        History.Header = "Connected to: < " + arg.RemoteEndPoint.ToString() + " >";
                    }
                    else
                    {
                        History.Header = "Conversation History";
                    }
                  
                };

            _tcpClient.Received += 
                (sender, arg) =>
                {
                    History.Transmissions.Append(arg.Message);
                };


            IpAddress = "127.0.0.1";
            PlainTextSendTypeSelected = true;
            History.Header = "Conversation History";
            Message = "";
        }

        #endregion

        #region Private Functions

        private async void Connect()
        {
            if (!ValidateConnect())
                return;

            try
            {
                await _tcpClient.ConnectAsync(IpAddress, Port);
            }
            catch(Exception ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
            }        
        }

        private void Disconnect()
        {
            _tcpClient.Disconnect();
        }

        private async void Send()
        {
            byte[] data = new byte[0];
            try
            {
                data = _parser.Parse(Message, SettingsUtils.GetEncoding());
            }
            catch(FormatException ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
                return;
            }

            try
            {
                Piece msg = new Piece(data, Piece.EType.Sent);

                PieceSendResult res = await _tcpClient.SendAsync(msg);
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

        private void SendTypeChanged()
        {
            if(PlainTextSendTypeSelected)
            {
                _parser = new PlainTextParser();
            }
            else
            {
                _parser = new HexParser();
            }
        }

        private bool ValidateConnect()
        {
            string error = null;
            if (HasError(nameof(IpAddress)))
                error = GetError(nameof(IpAddress));
            else if (HasError(nameof(Port)))
                error = GetError(nameof(Port));

            if(error != null)
            {
                DialogUtils.ShowErrorDialog(error);
                return false;
            }
           
            return true;
        }

        #endregion

    }
}
