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
    public class TcpServerViewModel : ObservableObject
    {
        private TcpServer _tcpServer;
        private IParser _parser;


        private ObservableCollection<InterfaceItem> _localInterfaces;
        public ObservableCollection<InterfaceItem> LocalInterfaces
        {
            get { return _localInterfaces; }
            set
            {
                if(_localInterfaces != value)
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

        private InterfaceItem _selectedInterface;
        public InterfaceItem SelectedInterface
        {
            get { return _selectedInterface; }
            set
            {
                if(_selectedInterface != value)
                {
                    _selectedInterface = value;
                    OnPropertyChanged(nameof(SelectedInterface));
                }
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
            _parser = new PlainTextParser(Encoding.Default);
            LocalInterfaces = new ObservableCollection<InterfaceItem>();

            _tcpServer.StatusChanged +=
                (sender, arg) =>
                {
                    if(arg.Status == TcpServerStatusEventArgs.EServerStatus.Started)
                    {
                        IsStarted = true;
                        History.Header = "Listening on: < " + arg.ServerInfo.ToString() + " >";
                    }
                    else if(arg.Status == TcpServerStatusEventArgs.EServerStatus.Stopped)
                    {
                        History.Header = "Conversation History";
                        IsStarted = false;
                    }
                    else if(arg.Status == TcpServerStatusEventArgs.EServerStatus.ClientConnected)
                    {
                        History.Header = "Connected client: < " + arg.ClientInfo.ToString() + " >";
                        IsClientConnected = true;
                    }
                    else if(arg.Status == TcpServerStatusEventArgs.EServerStatus.ClientDisconnected)
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


            PlainTextSendTypeSelected = true;
            History.Header = "Conversation History";


            // build interface list
            LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Any, IPAddress.Any));
            LocalInterfaces.Add(new InterfaceItem(InterfaceItem.EInterfaceType.Any, IPAddress.IPv6Any));
            foreach(var i in NetworkUtils.GetActiveInterfaces())
            {

                if(i.IPv4Address != null)
                {
                    LocalInterfaces.Add(new InterfaceItem(
                        InterfaceItem.EInterfaceType.Specific, i.IPv4Address));
                }

                if(i.IPv6Address != null)
                {
                    LocalInterfaces.Add(new InterfaceItem(
                        InterfaceItem.EInterfaceType.Specific, i.IPv6Address));
                }              
            }
        }


        private void Start()
        {
            try
            {
                _tcpServer.Start(SelectedInterface.Interface, Port);
            }
            catch(System.Net.Sockets.SocketException ex)
            {
                String message = ex.Message;

                if(ex.ErrorCode == 10013)
                {
                    message = "Port " + Port + " is already in use, unable to start server.";
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
                _parser = new PlainTextParser(Encoding.Default);
            }
            else
            {
                _parser = new HexParser();
            }
        }

    }
}
