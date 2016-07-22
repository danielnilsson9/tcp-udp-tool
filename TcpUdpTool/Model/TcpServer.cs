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

namespace TcpUdpTool.Model
{

    class TcpServer
    {
        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<ServerStatusEventArgs> StatusChanged;


        private TcpListener _tcpServer;
        private System.Net.Sockets.TcpClient _connectedClient;
        private byte[] _buffer;


        public TcpServer()
        {
            _buffer = new byte[8192];
        }


        public void Start(string ip, int port)
        {
            if(_tcpServer != null)
            {
                return;
            }
          
            try
            {
                _tcpServer = new TcpListener(new IPEndPoint(IPAddress.Parse(ip), port));
                _tcpServer.Start(0);
                OnStatusChanged(ServerStatusEventArgs.EServerStatus.Started);
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
                OnStatusChanged(ServerStatusEventArgs.EServerStatus.Stopped);
            }            
        }

        public async Task<PieceSendResult> SendAsync(Piece msg)
        {
            if(_connectedClient == null)
            {
                return null;
            }

            IPEndPoint from = _connectedClient.Client.LocalEndPoint as IPEndPoint;
            IPEndPoint to = _connectedClient.Client.RemoteEndPoint as IPEndPoint;

            await _connectedClient.GetStream().WriteAsync(msg.Data, 0, msg.Length);

            return new PieceSendResult() { From = from, To = to };
        }

        public void Disconnect()
        {
            // close client connection.
            if(_connectedClient != null)
            {
                EndPoint info = _connectedClient.Client.RemoteEndPoint;
                _connectedClient.Close();
                _connectedClient = null;
                OnStatusChanged(ServerStatusEventArgs.EServerStatus.ClientDisconnected);
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
                            OnStatusChanged(ServerStatusEventArgs.EServerStatus.ClientConnected);
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


                            Piece msg = new Piece(data, Piece.EType.Received);
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

        private void OnStatusChanged(ServerStatusEventArgs.EServerStatus status)
        {        
            StatusChanged?.Invoke(this, new ServerStatusEventArgs(status, 
                _tcpServer?.LocalEndpoint as IPEndPoint, 
                _connectedClient?.Client.RemoteEndPoint as IPEndPoint));
        }

    }

    public class ServerStatusEventArgs : System.EventArgs
    {
        public enum EServerStatus { Started, Stopped, ClientConnected, ClientDisconnected }

        public EServerStatus Status { get; private set; }
        public IPEndPoint ServerInfo { get; private set; }
        public IPEndPoint ClientInfo { get; private set; }


        public ServerStatusEventArgs(EServerStatus status, IPEndPoint serverInfo, IPEndPoint clientInfo = null)
        {
            Status = status;
            ServerInfo = serverInfo;
            ClientInfo = clientInfo;
        }
    }

}
