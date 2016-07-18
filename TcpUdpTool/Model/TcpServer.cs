using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model
{

    class TcpServer
    {
        public event Action<Piece> DataReceived;
        public event Action<bool> StartedStatusChanged;
        public event Action<bool, EndPoint> ConnectionStatusChanged;
        

        private TcpListener _tcpServer;
        private System.Net.Sockets.TcpClient _connectedClient;
        private bool _started = false;
        private byte[] _buffer;


        public TcpServer()
        {
            _buffer = new byte[8192];
        }


        public bool IsStarted()
        {
            return _started;
        }

        public bool IsClientConnected()
        {
            return _connectedClient != null && _connectedClient.Connected;
        }


        public void Start(string ip, int port)
        {
            if(_tcpServer != null)
            {
                return;
            }

            _tcpServer = new TcpListener(new IPEndPoint(IPAddress.Parse(ip), port));
            _tcpServer.Start(0);
            _started = true;
            StartedStatusChanged?.Invoke(true);

            AcceptClient();
        }

        public void Stop()
        {
            Disconnect();
            if(IsStarted())
            {
                _started = false;
                _tcpServer.Stop();
                _tcpServer = null;               
            }

            StartedStatusChanged?.Invoke(false);
        }

        public void Send(Piece msg)
        {
            if(!IsClientConnected())
            {
                return;
            }

            _connectedClient.GetStream().WriteAsync(msg.Data, 0, msg.Length);
        }


        public void Disconnect()
        {
            // close client connection.
            if(_connectedClient != null)
            {
                _connectedClient.Close();
                _connectedClient = null;            
            }

            ConnectionStatusChanged?.Invoke(false, null);
        }


        private void AcceptClient()
        {
            _tcpServer.BeginAcceptTcpClient(new AsyncCallback(
                (ar) =>
                {
                    if (_tcpServer == null)
                        return;

                    try
                    {
                        System.Net.Sockets.TcpClient client = _tcpServer.EndAcceptTcpClient(ar);

                        if(_connectedClient == null)
                        {
                            _connectedClient = client;
                            ConnectionStatusChanged?.Invoke(true, _connectedClient.Client.RemoteEndPoint);
                            Receive(_connectedClient);
                        }
                        else
                        {
                            // only one connection allowed, close this request.
                            client.Close();
                        }

                        AcceptClient();
                    }
                    catch(ObjectDisposedException)
                    {
                        // stopped
                    }             
                          
                }), null);
        }

        private void Receive(System.Net.Sockets.TcpClient client)
        {
            if (!client.Connected)
                return;

            client.GetStream().BeginRead(_buffer, 0, _buffer.Length, new AsyncCallback(
                (ar) =>
                {
                    if (_connectedClient == null)
                        return;

                    try
                    {
                        int read = client.GetStream().EndRead(ar);
                        if (read > 0)
                        {
                            byte[] data = new byte[read];
                            Array.Copy(_buffer, data, read);

                            DataReceived?.Invoke(new Piece(data, Piece.EType.Received, client.Client.RemoteEndPoint));

                            // read again
                            Receive(client);
                        }
                        else
                        {
                            // disconnect, server closed connection.
                            Disconnect();
                        }
                    }
                    catch (Exception e)
                    when (e is ObjectDisposedException || e is IOException || e is InvalidOperationException)
                    {
                        // disconnected
                        Disconnect();
                    }

                }), null);            
        }

    }
}
