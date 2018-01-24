using System;
using System.Net;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.EventArg;
using TcpUdpTool.Model.Util;

namespace TcpUdpTool.Model
{
    public class TcpClient : IDisposable
    {
      
        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<TcpClientStatusEventArgs> StatusChanged;

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

            OnConnectStatusChanged(TcpClientStatusEventArgs.EConnectStatus.Connecting);
        
            try
            {
                // resolve ip address
                IPAddress addr = await NetworkUtils.DnsResolveAsync(host);

                _tcpClient = new System.Net.Sockets.TcpClient(addr.AddressFamily);

                await _tcpClient.ConnectAsync(addr, port);
                OnConnectStatusChanged(TcpClientStatusEventArgs.EConnectStatus.Connected);

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
            }

            OnConnectStatusChanged(TcpClientStatusEventArgs.EConnectStatus.Disconnected);
        }

        public async Task<TransmissionResult> SendAsync(Transmission msg)
        {
            if(!_tcpClient.Connected)
            {
                return null;
            }

            IPEndPoint from = _tcpClient.Client.LocalEndPoint as IPEndPoint;
            IPEndPoint to = _tcpClient.Client.RemoteEndPoint as IPEndPoint;

            await _tcpClient.GetStream().WriteAsync(msg.Data, 0, msg.Length);

            return new TransmissionResult() { From = from, To = to };
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


                            Transmission msg = new Transmission(data, Transmission.EType.Received);
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

        private void OnConnectStatusChanged(TcpClientStatusEventArgs.EConnectStatus status)
        {
            IPEndPoint ep = status == TcpClientStatusEventArgs.EConnectStatus.Connected ? 
                _tcpClient.Client.RemoteEndPoint as IPEndPoint : null;

            StatusChanged?.Invoke(false, new TcpClientStatusEventArgs(status, ep));
        }

        public void Dispose()
        {
            _tcpClient?.Close();
            _tcpClient = null;
        }

    }

    public class TcpClientStatusEventArgs : EventArgs
    {
        public enum EConnectStatus { Disconnected, Connecting, Connected };


        public EConnectStatus Status { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }

        public TcpClientStatusEventArgs(EConnectStatus status, IPEndPoint remoteEndPoint)
        {
            Status = status;
            RemoteEndPoint = remoteEndPoint;
        }

    }

}
