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
using TcpUdpTool.ViewModel.Command;

namespace TcpUdpTool.ViewModel.Presenter
{
    public class TcpClientPresenter : ObservableObject
    {
        private TransmissionHistory _transmissionHistory;
        private IParser _parser;
        private TcpClient _tcpClient;


        private string _conversationHistory;
        public string ConversationHistory
        {
            get { return _conversationHistory; }
            set
            {
                _conversationHistory = value;
                OnPropertyChanged("ConversationHistory");
            }
        }

        private bool _plainTextHistorySelected;
        public bool PlainTextHistorySelected
        {
            get { return _plainTextHistorySelected; }
            set
            {
                if(value != _plainTextHistorySelected)
                {
                    _plainTextHistorySelected = value;
                    OnPropertyChanged("PlainTextHistorySelected");
                }
            }
        }

        private bool _hexHistorySelected;
        public bool HexHistorySelected
        {
            get { return _hexHistorySelected; }
            set
            {
                if (value != _hexHistorySelected)
                {
                    _hexHistorySelected = value;
                    OnPropertyChanged("HextHistorySelected");
                }
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


        public string History
        {
            get { return _transmissionHistory.Get(); }
            set { }
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




        public ICommand ConnectCommand
        {
            get { return new DelegateCommand(Connect); }

        }

        public ICommand DisconnectCommand
        {
            get { return new DelegateCommand(Disconnect); }
        }

        public ICommand SendCommand
        {
            get { return new DelegateCommand(Send); }
        }

        public ICommand SaveCommand
        {
            get { return new DelegateCommand(Save); }
        }

        public ICommand ClearCommand
        {
            get { return new DelegateCommand(Clear); }
        }

        public ICommand HistoryViewChangedCommand
        {
            get { return new DelegateCommand(HistoryViewChanged); }
        }

        public ICommand SendTypeChangedCommand
        {
            get { return new DelegateCommand(SendTypeChanged); }
        }



        public TcpClientPresenter()
        {
            _transmissionHistory = new TransmissionHistory();
            _tcpClient = new TcpClient();

            _tcpClient.ConnectStatusChanged += 
                (connected) => 
                {
                    IsConnected = connected;
                };

            _tcpClient.DataReceived += 
                (msg) =>
                {
                    _transmissionHistory.Append(msg);
                };

            _transmissionHistory.HistoryChanged += 
                () =>
                {
                    // Trigger change event so view gets updated.
                    OnPropertyChanged("History");
                };

            IpAddress = "127.0.0.1";
            PlainTextHistorySelected = true;
            PlainTextSendTypeSelected = true;
            _parser = new PlainTextParser();
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
            _transmissionHistory.Append(msg);

            Message = "";
        }

        private void Save()
        {

        }

        private void Clear()
        {
            _transmissionHistory.Clear();
        }


        private void HistoryViewChanged()
        {           
            if(PlainTextHistorySelected)
            {
                _transmissionHistory.SetFormatter(new PlainTextFormatter());
            }
            else if(HexHistorySelected)
            {
                _transmissionHistory.SetFormatter(new HexFormatter());
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

    }
}
