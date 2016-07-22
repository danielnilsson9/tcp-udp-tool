using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Parser;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class UdpMulticastViewModel : ObservableObject
    {
        private UdpMulticastClient _udpClient;
        private IParser _parser;


        private HistoryViewModel _historyViewModel = new HistoryViewModel();
        public HistoryViewModel History
        {
            get { return _historyViewModel; }
        }

        private bool _isGroupJoined;
        public bool IsGroupJoined
        {
            get { return _isGroupJoined; }
            set
            {
                _isGroupJoined = value;
                OnPropertyChanged(nameof(IsGroupJoined));
            }
        }

        private string _multicastGroup;
        public string MulticastGroup
        {
            get { return _multicastGroup; }
            set
            {
                _multicastGroup = value;
                OnPropertyChanged(nameof(MulticastGroup));
            }
        }

        private int _multicastPort;
        public int MulticastPort
        {
            get { return _multicastPort; }
            set
            {
                _multicastPort = value;
                OnPropertyChanged(nameof(MulticastPort));
            }
        }

        private string _sendMulticastGroup;
        public string SendMulticastGroup
        {
            get { return _sendMulticastGroup; }
            set
            {
                _sendMulticastGroup = value;
                OnPropertyChanged(nameof(SendMulticastGroup));
            }
        }

        private int _sendMulticastPort;
        public int SendMulticastPort
        {
            get { return _sendMulticastPort; }
            set
            {
                _sendMulticastPort = value;
                OnPropertyChanged(nameof(SendMulticastPort));
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
                if (value != _plainTextSendTypeSelected)
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




        public ICommand JoinLeaveCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (IsGroupJoined)
                    {
                        Leave();
                    }
                    else
                    {
                        Join();
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



        public UdpMulticastViewModel()
        {
            _udpClient = new UdpMulticastClient();
            _parser = new PlainTextParser();

            _udpClient.Received +=
                (sender, arg) =>
                {
                    History.Transmissions.Append(arg.Message);
                };

            _udpClient.StatusChanged += 
                (sender, arg) =>
                {
                    IsGroupJoined = arg.Joined;
                };

            PlainTextSendTypeSelected = true;

        }


        private void Join()
        {
            _udpClient.Join(IPAddress.Parse(MulticastGroup), MulticastPort, UdpMulticastClient.EMulticastInterface.All);
        }

        private void Leave()
        {
            _udpClient.Leave();
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
            PieceSendResult res = await _udpClient.SendAsync(msg, IPAddress.Parse(SendMulticastGroup), 
                SendMulticastPort, UdpMulticastClient.EMulticastInterface.All);

            if(res != null)
            {
                msg.Origin = res.From;
                msg.Destination = res.To;
                History.Transmissions.Append(msg);
            }

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
