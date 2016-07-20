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
    public class UdpClientServer
    { 

        public event Action<Piece> DataReceived;
        public event Action<bool, EndPoint> ServerStatusChanged;


        private UdpClient _udpClient;
        private UdpClient _sendUdpClient;
        private bool _started = false;


        public UdpClientServer()
        {
            _sendUdpClient = new UdpClient();
            _sendUdpClient.EnableBroadcast = true;
        }


        public void Start(string ip, int port)
        {
            if (_udpClient != null)
                return;

            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), port));
            _udpClient.EnableBroadcast = true;
            _started = true;

            ServerStatusChanged?.Invoke(true, _udpClient.Client.LocalEndPoint);

            Receive();
        }

        public bool IsStarted()
        {
            return _started;
        }

        public void Stop()
        {
            if(_udpClient != null)
            {
                _udpClient.Close();
                _udpClient = null;
            }

            _started = false;
            ServerStatusChanged?.Invoke(false, null);
        }
        
        public void Send(string ip, int port, Piece msg)
        {
            _sendUdpClient.SendAsync(msg.Data, msg.Data.Length, ip, port);                   
        }


        private void Receive()
        {
            if (_udpClient == null)
                return;

            _udpClient.BeginReceive(
                new AsyncCallback((ar) =>
                {
                    if (_udpClient == null)
                        return;

                    try
                    {
                        IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = _udpClient.EndReceive(ar, ref from);

                        DataReceived?.Invoke(new Piece(data, Piece.EType.Received, from));

                        // receive again
                        Receive();
                    }
                    catch (Exception e)
                    when (e is ObjectDisposedException || e is IOException || e is InvalidOperationException)
                    {
                        // error, probably already stoppped.
                        Stop();
                    }
                }), null);
        }

    }
}
