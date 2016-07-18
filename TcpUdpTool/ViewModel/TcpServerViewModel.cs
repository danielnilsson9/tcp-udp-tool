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

        public bool IsStarted
        {
            get { return _tcpServer.IsStarted(); }
        }

        public bool IsClientConnected
        {
            get { return _tcpServer.IsClientConnected(); }
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

            _tcpServer.StartedStatusChanged +=
                (started) =>
                {
                    OnPropertyChanged("IsStarted");
                };

            _tcpServer.ConnectionStatusChanged +=
                (connected, clientEp) =>
                {
                    OnPropertyChanged("IsClientConnected");
                    History.Header = "Connected client: < " + (connected ? clientEp.ToString() : "NONE") + " >";
                };

            _tcpServer.DataReceived +=
                (msg) =>
                {
                    History.Transmissions.Append(msg);
                };


            IpAddress = "0.0.0.0";
            PlainTextSendTypeSelected = true;
            History.Header = "Connected client: < NONE >";
        }


        private void Start()
        {
            _tcpServer.Start(IpAddress, Port);
        }

        private void Stop()
        {
            _tcpServer.Stop();
        }

        private void Send()
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

            Piece msg = new Piece(data, Piece.EType.Sent, null);

            _tcpServer.Send(msg);
            History.Transmissions.Append(msg);

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
