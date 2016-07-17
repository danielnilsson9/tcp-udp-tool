using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.ViewModel.Command;

namespace TcpUdpTool.ViewModel.Presenter
{
    public class TcpClientPresenter : ObservableObject
    {
        private TransmissionHistory _transmissionHistory;
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
            Piece msg = new Piece(Encoding.UTF8.GetBytes(Message), Piece.EType.Sent);

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

    }
}
