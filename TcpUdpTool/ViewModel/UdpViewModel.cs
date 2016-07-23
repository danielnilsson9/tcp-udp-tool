using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using static TcpUdpTool.Model.UdpClientServerStatusEventArgs;

namespace TcpUdpTool.ViewModel
{
    public class UdpViewModel : ObservableObject
    {

        private UdpClientServer _udpClientServer;
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

        private bool _isServerStarted;
        public bool IsServerStarted
        {
            get { return _isServerStarted; }
            set
            {
                if(_isServerStarted != value)
                {
                    _isServerStarted = value;
                    OnPropertyChanged(nameof(IsServerStarted));
                }
            }
        }

        private InterfaceItem _selectedInterface;
        public InterfaceItem SelectedInterface
        {
            get { return _selectedInterface; }
            set
            {
                if (_selectedInterface != value)
                {
                    _selectedInterface = value;
                    OnPropertyChanged(nameof(SelectedInterface));
                }
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
            _parser = new PlainTextParser(Encoding.Default);
            LocalInterfaces = new ObservableCollection<InterfaceItem>();

            _udpClientServer.StatusChanged +=
                (sender, arg) =>
                {
                    IsServerStarted = (arg.ServerStatus == EServerStatus.Started);

                    if (IsServerStarted)
                    {
                        History.Header = "Listening on: < " + arg.ServerInfo.ToString() + " >";
                    }
                    else
                    {
                        History.Header = "Conversation History";
                    }
                };

            _udpClientServer.Received +=
                (sender, arg) =>
                {
                    History.Transmissions.Append(arg.Message);
                };

            PlainTextSendTypeSelected = true;
            History.Header = "Conversation History";

            // build interface list
            LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Any, IPAddress.Any));
            LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Any, IPAddress.IPv6Any));
            foreach (var i in NetworkUtils.GetActiveInterfaces())
            {

                if (i.IPv4Address != null)
                {
                    LocalInterfaces.Add(new InterfaceItem(
                        InterfaceItem.EInterfaceType.Specific, i.IPv4Address));
                }

                if (i.IPv6Address != null)
                {
                    LocalInterfaces.Add(new InterfaceItem(
                        InterfaceItem.EInterfaceType.Specific, i.IPv6Address));
                }
            }
        }


        private void Start()
        {
            _udpClientServer.Start(SelectedInterface.Interface, ListenPort);
        }

        private void Stop()
        {
            _udpClientServer.Stop();
        }

        private async void Send()
        {
            byte[] data = new byte[0];
            try
            {
                data = _parser.Parse(Message);
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            Piece msg = new Piece(data, Piece.EType.Sent);

            try
            {
                PieceSendResult res = await _udpClientServer.SendAsync(SendIpAddress, SendPort, msg);
                if(res != null)
                {
                    msg.Origin = res.From;
                    msg.Destination = res.To;

                    History.Transmissions.Append(msg);
                }
            }
            catch(InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error");
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

    }
}
