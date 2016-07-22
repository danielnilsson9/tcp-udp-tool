using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Parser;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class TcpServerViewModel : ObservableObject
    {
        private TcpServer _tcpServer;
        private IParser _parser;


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


        public TcpServerViewModel()
        {
            _tcpServer = new TcpServer();
            _parser = new PlainTextParser();

            _tcpServer.StatusChanged +=
                (sender, arg) =>
                {
                    if(arg.Status == ServerStatusEventArgs.EServerStatus.Started)
                    {
                        IsStarted = true;
                        History.Header = "Listening on: < " + arg.ServerInfo.ToString() + " >";
                    }
                    else if(arg.Status == ServerStatusEventArgs.EServerStatus.Stopped)
                    {
                        History.Header = "Conversation History";
                        IsStarted = false;
                    }
                    else if(arg.Status == ServerStatusEventArgs.EServerStatus.ClientConnected)
                    {
                        History.Header = "Connected client: < " + arg.ClientInfo.ToString() + " >";
                        IsClientConnected = true;
                    }
                    else if(arg.Status == ServerStatusEventArgs.EServerStatus.ClientDisconnected)
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


            IpAddress = "0.0.0.0";
            PlainTextSendTypeSelected = true;
            History.Header = "Conversation History";
        }


        private void Start()
        {
            try
            {
                _tcpServer.Start(IpAddress, Port);
            }
            catch(System.Net.Sockets.SocketException ex)
            {
                String message = ex.Message;

                if(ex.ErrorCode == 10013)
                {
                    message = "Unable to start server on port " + Port + ", already in use.";
                }
                else if(ex.ErrorCode == 10049)
                {
                    message = "Unable to bind to " + IpAddress + ", address is not valid on this computer.";
                }

                MessageBox.Show(message, "Error");
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
                data = _parser.Parse(Message);
            }
            catch (FormatException e)
            {
                MessageBox.Show(e.Message, "Error");
                return;
            }

            Piece msg = new Piece(data, Piece.EType.Sent);

            PieceSendResult res = await _tcpServer.SendAsync(msg);
            if(res != null)
            {
                msg.Origin = res.From;
                msg.Destination = res.To;
                History.Transmissions.Append(msg);
            }
            
            Message = "";
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

    }
}
