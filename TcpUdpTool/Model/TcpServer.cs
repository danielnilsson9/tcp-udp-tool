using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.EventArg;

namespace TcpUdpTool.Model
{

    public class TcpServer : IDisposable
    {
        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<TcpServerStatusEventArgs> StatusChanged;


        private TcpListener _tcpServer;
        private System.Net.Sockets.TcpClient _connectedClient;
        private byte[] _buffer;


        public TcpServer()
        {
            _buffer = new byte[8192];
        }


        public void Start(IPAddress ip, int port)
        {
            if(_tcpServer != null)
                return;
                  
            try
            {
                _tcpServer = new TcpListener(new IPEndPoint(ip, port));
                _tcpServer.Start(0);
                OnStatusChanged(TcpServerStatusEventArgs.EServerStatus.Started);
            }
            catch(Exception)
            {
                Stop();
                throw;
            }
            
            StartAcceptClient();
        }

        public void Stop()
        {
            Disconnect();
            if(_tcpServer != null)
            {
                _tcpServer.Stop();
                _tcpServer = null;
                OnStatusChanged(TcpServerStatusEventArgs.EServerStatus.Stopped);
            }            
        }

        public async Task<TransmissionResult> SendAsync(Transmission msg)
        {
            if(_connectedClient == null)
            {
                return null;
            }

            IPEndPoint from = _connectedClient.Client.LocalEndPoint as IPEndPoint;
            IPEndPoint to = _connectedClient.Client.RemoteEndPoint as IPEndPoint;

            await _connectedClient.GetStream().WriteAsync(msg.Data, 0, msg.Length);

            return new TransmissionResult() { From = from, To = to };
        }

        public void Disconnect()
        {
            // close client connection.
            if(_connectedClient != null)
            {
                _connectedClient.Close();
                _connectedClient = null;
                OnStatusChanged(TcpServerStatusEventArgs.EServerStatus.ClientDisconnected);
            }  
        }


        private void StartAcceptClient()
        {
            Task.Run(async () =>
            {
                while(_tcpServer != null)
                {
                    try
                    {
                        System.Net.Sockets.TcpClient client = await _tcpServer.AcceptTcpClientAsync();

                        if (_connectedClient == null)
                        {
                            _connectedClient = client;
                            OnStatusChanged(TcpServerStatusEventArgs.EServerStatus.ClientConnected);
                            StartReceive();
                        }
                        else
                        {
                            // only one connection allowed, close this request.
                            client.Close();
                        }
                    }
                    catch(Exception ex)
                    when(ex is SocketException || ex is ObjectDisposedException)
                    {
                        Stop();
                        break;
                    }
                }
            });
        }

        private void StartReceive()
        {
            Task.Run(async () =>
            {
                while (_connectedClient != null)
                {
                    try
                    {
                        int read = await _connectedClient.GetStream().ReadAsync(_buffer, 0, _buffer.Length);

                        if (read > 0)
                        {
                            byte[] data = new byte[read];
                            Array.Copy(_buffer, data, read);


                            Transmission msg = new Transmission(data, Transmission.EType.Received);
                            msg.Destination = _connectedClient.Client.LocalEndPoint as IPEndPoint;
                            msg.Origin = _connectedClient.Client.RemoteEndPoint as IPEndPoint;

                            Received?.Invoke(this, new ReceivedEventArgs(msg));
                        }
                        else
                        {
                            // server closed connection.
                            Disconnect();
                            break;
                        }
                    }
                    catch (Exception e)
                    when (e is ObjectDisposedException || e is InvalidOperationException)
                    {
                        Disconnect();
                        break;
                    }
                }
            });    
        }

        private void OnStatusChanged(TcpServerStatusEventArgs.EServerStatus status)
        {        
            StatusChanged?.Invoke(this, new TcpServerStatusEventArgs(status, 
                _tcpServer?.LocalEndpoint as IPEndPoint, 
                _connectedClient?.Client.RemoteEndPoint as IPEndPoint));
        }

        public void Dispose()
        {
            _tcpServer?.Stop();
            _tcpServer = null;
            _connectedClient?.Close();
            _connectedClient = null;
        }
    }

    public class TcpServerStatusEventArgs : EventArgs
    {
        public enum EServerStatus { Started, Stopped, ClientConnected, ClientDisconnected }

        public EServerStatus Status { get; private set; }
        public IPEndPoint ServerInfo { get; private set; }
        public IPEndPoint ClientInfo { get; private set; }


        public TcpServerStatusEventArgs(EServerStatus status, IPEndPoint serverInfo, IPEndPoint clientInfo = null)
        {
            Status = status;
            ServerInfo = serverInfo;
            ClientInfo = clientInfo;
        }
    }

}
