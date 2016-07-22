using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.EventArgs;
using TcpUdpTool.Model.Util;

namespace TcpUdpTool.Model
{
    public class TcpClient
    {
      
        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<ClientStatusEventArgs> StatusChanged;

        private System.Net.Sockets.TcpClient _tcpClient;
        private byte[] _buffer;

        
        public TcpClient()
        {           
            _buffer = new byte[8192];            
        }


        public async Task ConnectAsync(string host, int port)
        {
            if (_tcpClient != null && _tcpClient.Connected)
                return; // already connected

            OnConnectStatusChanged(ClientStatusEventArgs.EConnectStatus.Connecting);
        
            try
            {
                // resolve ip address
                IPAddress addr = await NetworkUtils.DnsResolveAsync(host);

                _tcpClient = new System.Net.Sockets.TcpClient(addr.AddressFamily);

                await _tcpClient.ConnectAsync(addr, port);
                OnConnectStatusChanged(ClientStatusEventArgs.EConnectStatus.Connected);

                StartReceive();
            }
            catch(Exception)
            {
                Disconnect();
                throw;
            }   
        }

        public void Disconnect()
        {
            if(_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
                OnConnectStatusChanged(ClientStatusEventArgs.EConnectStatus.Disconnected);
            }
        }

        public async Task<PieceSendResult> SendAsync(Piece msg)
        {
            if(!_tcpClient.Connected)
            {
                return null;
            }

            IPEndPoint from = _tcpClient.Client.LocalEndPoint as IPEndPoint;
            IPEndPoint to = _tcpClient.Client.RemoteEndPoint as IPEndPoint;

            await _tcpClient.GetStream().WriteAsync(msg.Data, 0, msg.Length);

            return new PieceSendResult() { From = from, To = to };
        }


        private void StartReceive()
        {
            Task.Run(async () =>
            {
                while(_tcpClient != null)
                {
                    try
                    {
                        int read = await _tcpClient.GetStream().ReadAsync(_buffer, 0, _buffer.Length);

                        if(read > 0)
                        {
                            byte[] data = new byte[read];
                            Array.Copy(_buffer, data, read);


                            Piece msg = new Piece(data, Piece.EType.Received);
                            msg.Destination = _tcpClient.Client.LocalEndPoint as IPEndPoint;
                            msg.Origin = _tcpClient.Client.RemoteEndPoint as IPEndPoint;

                            Received?.Invoke(this, new ReceivedEventArgs(msg));
                        }
                        else
                        {
                            // server closes connection.
                            Disconnect();
                            break;
                        }
                    }
                    catch(Exception e)
                    when(e is ObjectDisposedException || e is InvalidOperationException)
                    {
                        Disconnect();
                        break;
                    }
                }
            });
        }

        private void OnConnectStatusChanged(ClientStatusEventArgs.EConnectStatus status)
        {
            IPEndPoint ep = status == ClientStatusEventArgs.EConnectStatus.Connected ? 
                _tcpClient.Client.RemoteEndPoint as IPEndPoint : null;

            StatusChanged?.Invoke(false, new ClientStatusEventArgs(status, ep));
        }

    }

    public class ClientStatusEventArgs : System.EventArgs
    {
        public enum EConnectStatus { Disconnected, Connecting, Connected };


        public EConnectStatus Status { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }

        public ClientStatusEventArgs(EConnectStatus status, IPEndPoint remoteEndPoint)
        {
            Status = status;
            RemoteEndPoint = remoteEndPoint;
        }

    }

}
