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
    public class UdpViewModel : ObservableObject
    {

        private UdpClientServer _udpClientServer;
        private IParser _parser;

        private HistoryViewModel _historyViewModel = new HistoryViewModel();
        public HistoryViewModel History
        {
            get { return _historyViewModel; }
        }


        public bool IsServerStarted
        {
            get { return _udpClientServer.IsStarted(); }
        }

        private string _listenIpAddress;
        public string ListenIpAddress
        {
            get { return _listenIpAddress; }
            set
            {
                _listenIpAddress = value;
                OnPropertyChanged("ListenIpAddress");
            }
        }

        private int _listenPort;
        public int ListenPort
        {
            get { return _listenPort; }
            set
            {
                _listenPort = value;
                OnPropertyChanged("ListenPort");
            }
        }


        private string _sendIpAddress;
        public string SendIpAddress
        {
            get { return _sendIpAddress; }
            set
            {
                _sendIpAddress = value;
                OnPropertyChanged("SendIpAddress");
            }
        }

        private int _sendPort;
        public int SendPort
        {
            get { return _sendPort; }
            set
            {
                _sendPort = value;
                OnPropertyChanged("SendPort");
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


        public UdpViewModel()
        {
            _udpClientServer = new UdpClientServer();
            _parser = new PlainTextParser();

            _udpClientServer.ServerStatusChanged +=
                (started, localEp) =>
                {
                    OnPropertyChanged("IsServerStarted");

                    if (started)
                    {
                        History.Header = "Listening on: < " + localEp.ToString() + " >";
                    }
                    else
                    {
                        History.Header = "Conversation History";
                    }
                };


            _udpClientServer.DataReceived +=
                (msg) =>
                {
                    History.Transmissions.Append(msg);
                };

            ListenIpAddress = "0.0.0.0";
            PlainTextSendTypeSelected = true;

            History.Header = "Conversation History";
        }


        private void Start()
        {
            _udpClientServer.Start(ListenIpAddress, ListenPort);
        }

        private void Stop()
        {
            _udpClientServer.Stop();
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

            _udpClientServer.Send(SendIpAddress, SendPort, msg);
            History.Transmissions.Append(msg);

            Message = "";
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
