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
                OnPropertyChanged("IsConnected");
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

            _tcpClient.ConnectStatusChanged += 
                (connected, remoteEp) => 
                {
                    IsConnected = connected;
                    History.Header = "Connected to: < " + (connected ? remoteEp.ToString() : "NONE") + " >"; 
                };

            _tcpClient.DataReceived += 
                (msg) =>
                {
                    History.Transmissions.Append(msg);
                };


            IpAddress = "127.0.0.1";
            PlainTextSendTypeSelected = true;
            History.Header = "Connected to: < NONE >";   
        }



        private void Connect()
        {
            _tcpClient.Connect(IpAddress, Port);
        }

        private void Disconnect()
        {
            _tcpClient.Disconnect();
        }

        private void Send()
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

            Piece msg = new Piece(data, Piece.EType.Sent, null);

            _tcpClient.Send(msg);
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
