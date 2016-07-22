using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Util;

namespace TcpUdpTool.Model
{
    public class UdpClientServer
    { 

        public event Action<Piece> DataReceived;
        public event Action<bool, EndPoint> ServerStatusChanged;


        private UdpClient _udpClient;
        private bool _started = false;


        public UdpClientServer()
        {

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
        
        public async Task<PieceSendResult> SendAsync(string host, int port, Piece msg)
        {
            IPAddress addr = await NetworkUtils.DnsResolveAsync(host);
            IPEndPoint from = null;
            IPEndPoint to = new IPEndPoint(addr, port);

            if(_udpClient != null)
            {
                from = _udpClient.Client.LocalEndPoint as IPEndPoint;
                await _udpClient.SendAsync(msg.Data, msg.Data.Length, to);
            }
            else
            {
                // send from new udp client, don't care about any response
                // since we are not listening.
                using (UdpClient tmpClient = new UdpClient(addr.AddressFamily))
                {
                    from = tmpClient.Client.LocalEndPoint as IPEndPoint;
                    tmpClient.EnableBroadcast = true;
                    await tmpClient.SendAsync(msg.Data, msg.Data.Length, to);
                }
            }

            return new PieceSendResult() { From = from, To = to };            
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

                        Piece msg = new Piece(data, Piece.EType.Received);
                        msg.Origin = from;
                        msg.Destination = _udpClient.Client.LocalEndPoint as IPEndPoint;

                        DataReceived?.Invoke(msg);

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
