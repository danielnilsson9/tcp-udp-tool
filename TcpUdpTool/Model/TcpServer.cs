using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<System.Net.Sockets.TcpClient> _connectedClients;
        private byte[] _buffer;


        public int NumConnectedClients
		{
            get 
            {
                int count = 0;
                lock(_connectedClients)
				{
                    count = _connectedClients.Count;
				}

                return count;
            }
		}


        public TcpServer()
        {
            _connectedClients = new List<System.Net.Sockets.TcpClient>();
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

        public async Task<List<TransmissionResult>> SendAsync(Transmission msg)
        {
            var result = new List<TransmissionResult>();

            List<System.Net.Sockets.TcpClient> copy;
            lock (_connectedClients)
			{
                copy = _connectedClients.ToList();
            }

            foreach (var c in copy)
            {
                IPEndPoint from = c.Client.LocalEndPoint as IPEndPoint;
                IPEndPoint to = c.Client.RemoteEndPoint as IPEndPoint;

				try
				{
                    await c.GetStream().WriteAsync(msg.Data, 0, msg.Length);
                    result.Add(new TransmissionResult { From = from, To = to });
                }
                catch(Exception)
				{
                    DisconnectClient(c);
				}
            }

            return result;
        }

        public void Disconnect()
        {
            // close client connection.

            List<System.Net.Sockets.TcpClient> copy;
            lock(_connectedClients)
			{
                copy = _connectedClients.ToList();
			}

            foreach(var c in copy)
			{
                OnClientStatusChanged(TcpServerStatusEventArgs.EServerStatus.ClientDisconnected, c);
                c.Close();
                c.Dispose();
            }

            lock(_connectedClients)
			{
                _connectedClients.Clear();
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
                        var client = await _tcpServer.AcceptTcpClientAsync();
                        lock(_connectedClients)
						{
                            _connectedClients.Add(client);
                        }                    
                        OnClientStatusChanged(TcpServerStatusEventArgs.EServerStatus.ClientConnected, client);
                        StartReceive(client);
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

        private void StartReceive(System.Net.Sockets.TcpClient client)
        {
            Task.Run(async () =>
            {
                bool stop = false;
                while (!stop)
				{
                    try
                    {
                        int read = await client.GetStream().ReadAsync(_buffer, 0, _buffer.Length);

                        if (read > 0)
                        {
                            byte[] data = new byte[read];
                            Array.Copy(_buffer, data, read);

                            Transmission msg = new Transmission(data, Transmission.EType.Received);
                            msg.Destination = client.Client.LocalEndPoint as IPEndPoint;
                            msg.Origin = client.Client.RemoteEndPoint as IPEndPoint;

                            Received?.Invoke(this, new ReceivedEventArgs(msg));
                        }
                        else
                        {
                            // server closed connection.
                            DisconnectClient(client);
                            stop = true;
                        }
                    }
                    catch (Exception e)
                    when (e is ObjectDisposedException || e is InvalidOperationException)
                    {
                        DisconnectClient(client);
                        stop = true;
                    }
                }
            });    
        }

        private void OnStatusChanged(TcpServerStatusEventArgs.EServerStatus status)
        {        
            StatusChanged?.Invoke(this, new TcpServerStatusEventArgs(status, 
                _tcpServer?.LocalEndpoint as IPEndPoint, null));
        }

        private void OnClientStatusChanged(TcpServerStatusEventArgs.EServerStatus status, System.Net.Sockets.TcpClient client)
		{
            StatusChanged?.Invoke(this, new TcpServerStatusEventArgs(status,
                _tcpServer?.LocalEndpoint as IPEndPoint, client.Client.RemoteEndPoint as IPEndPoint));
        }

        private void DisconnectClient(System.Net.Sockets.TcpClient client)
		{
            lock(_connectedClients)
            {
                _connectedClients.Remove(client);
            }

            OnClientStatusChanged(TcpServerStatusEventArgs.EServerStatus.ClientDisconnected, client);
            client.Close();
            client.Dispose();
        }

        public void Dispose()
        {
            lock (_connectedClients)
            {
                foreach (var c in _connectedClients)
                {
                    c.Close();
                    c.Dispose();
                }

                _connectedClients.Clear();
            }

            _tcpServer?.Stop();
            _tcpServer = null;
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
