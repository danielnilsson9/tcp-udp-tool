using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TcpUdpTool.ViewModel.Item;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class UdpMulticastViewModel : ObservableObject
    {
        private UdpMulticastClient _udpClient;
        private IParser _parser;


        private ObservableCollection<InterfaceItem> _localInterfaces;
        public ObservableCollection<InterfaceItem> LocalInterfaces
        {
            get { return _localInterfaces; }
            set
            {
                if (_localInterfaces != value)
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

        private InterfaceItem _selectedListenInterface;
        public InterfaceItem SelectedListenInterface
        {
            get { return _selectedListenInterface; }
            set
            {
                if (_selectedListenInterface != value)
                {
                    _selectedListenInterface = value;
                    OnPropertyChanged(nameof(SelectedListenInterface));
                }
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

        private InterfaceItem _selectedSendInterface;
        public InterfaceItem SelectedSendInterface
        {
            get { return _selectedSendInterface; }
            set
            {
                if (_selectedSendInterface != value)
                {
                    _selectedSendInterface = value;
                    OnPropertyChanged(nameof(SelectedSendInterface));
                }
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
            _parser = new PlainTextParser(Encoding.Default);
            LocalInterfaces = new ObservableCollection<InterfaceItem>();

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

            // build interface list
            LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Default));
            LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.All));
            foreach (var i in NetworkUtils.GetActiveInterfaces())
            {
                if (i.IPv4Address != null)
                {
                    LocalInterfaces.Add(new InterfaceItem(
                        InterfaceItem.EInterfaceType.Specific, i.IPv4Address));
                }

                if(i.IPv6Address != null)
                {
                    LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Specific, i.IPv6Address));
                }
            }
        }


        private void Join()
        {
            _udpClient.Join(IPAddress.Parse(MulticastGroup), MulticastPort, 
                ToEMulticastInterface(SelectedListenInterface.Type), 
                SelectedListenInterface.Interface);
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
            PieceSendResult res = await _udpClient.SendAsync(
                msg, IPAddress.Parse(SendMulticastGroup), 
                SendMulticastPort, ToEMulticastInterface(SelectedSendInterface.Type), 
                SelectedSendInterface.Interface);

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
                _parser = new PlainTextParser(Encoding.Default);
            }
            else
            {
                _parser = new HexParser();
            }
        }


        private UdpMulticastClient.EMulticastInterface ToEMulticastInterface(InterfaceItem.EInterfaceType type)
        {
            UdpMulticastClient.EMulticastInterface res;
            switch (type)
            {
                case InterfaceItem.EInterfaceType.Default:
                    res = UdpMulticastClient.EMulticastInterface.Default;
                    break;
                case InterfaceItem.EInterfaceType.All:
                    res = UdpMulticastClient.EMulticastInterface.All;
                    break;
                default:
                    res = UdpMulticastClient.EMulticastInterface.Specific;
                    break;
            }

            return res;
        }

    }
}
