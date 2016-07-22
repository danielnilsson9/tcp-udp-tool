using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Formatter;
using TcpUdpTool.Model.Parser;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class TcpClientViewModel : ObservableObject
    {
       
        private IParser _parser;
        private TcpClient _tcpClient;


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
                _ipAddress = value;
                OnPropertyChanged("IpAddress");
            }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
                OnPropertyChanged("Port");
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
                if(value != _plainTextSendTypeSelected)
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
        }



        private async void Connect()
        {
            try
            {
                await _tcpClient.ConnectAsync(IpAddress, Port);
            }
            catch(Exception ex)
            when(ex is System.Net.Sockets.SocketException || ex is TimeoutException)
            {
                MessageBox.Show(ex.Message, "Error");
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
                data = _parser.Parse(Message);
            }
            catch(FormatException e)
            {
                MessageBox.Show(e.Message, "Error");
                return;
            }

            Piece msg = new Piece(data, Piece.EType.Sent);

            PieceSendResult res = await _tcpClient.SendAsync(msg);
            msg.Origin = res.From;
            msg.Destination = res.To;
            History.Transmissions.Append(msg);

            Message = "";
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

    }
}
