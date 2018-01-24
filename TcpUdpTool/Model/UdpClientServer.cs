using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.EventArg;
using TcpUdpTool.Model.Util;

namespace TcpUdpTool.Model
{
    public class UdpClientServer : IDisposable
    {
        private UdpClient _udpClient;

        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<UdpClientServerStatusEventArgs> StatusChanged;

      
        public UdpClientServer()
        {
            
        }

        public void Start(IPAddress ip, int port)
        {
            if (_udpClient != null)
                return;

            _udpClient = new UdpClient(ip.AddressFamily);            
            _udpClient.EnableBroadcast = true;

            _udpClient.Client.SetSocketOption(
                SocketOptionLevel.Socket, 
                SocketOptionName.ReuseAddress, 
                true
            );

            _udpClient.Client.Bind(new IPEndPoint(ip, port));

            StatusChanged?.Invoke(this, new UdpClientServerStatusEventArgs(
                UdpClientServerStatusEventArgs.EServerStatus.Started,
                _udpClient.Client.LocalEndPoint as IPEndPoint));

            StartReceive();
        }

        public void Stop()
        {
            if(_udpClient != null)
            {
                _udpClient.Close();
                _udpClient = null;

                StatusChanged?.Invoke(this, new UdpClientServerStatusEventArgs(
                    UdpClientServerStatusEventArgs.EServerStatus.Stopped));
            }
        }
        
        public async Task<TransmissionResult> SendAsync(string host, int port, Transmission msg)
        { 
            // resolve, prefer ipv6 if currently in use.
            IPAddress addr = await NetworkUtils.DnsResolveAsync(host, _udpClient != null && 
                _udpClient.Client.LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6); 

            IPEndPoint from = null;
            IPEndPoint to = new IPEndPoint(addr, port);

            if(_udpClient != null)
            {
                if(_udpClient.Client.AddressFamily != addr.AddressFamily)
                {
                    Func<AddressFamily, string> IpvToString = (family) =>
                    {
                        return family == AddressFamily.InterNetworkV6 ? "IPv6" : "IPv4";
                    };

                    throw new InvalidOperationException(
                        "Cannot send UDP packet using " + IpvToString(addr.AddressFamily) + 
                        " when bound to an " + IpvToString(_udpClient.Client.AddressFamily) + " interface.");
                }

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

            return new TransmissionResult() { From = from, To = to };            
        }


        private void StartReceive()
        {
            Task.Run(async () =>
            {
                while (_udpClient != null)
                {
                    try
                    {
                        UdpReceiveResult res = await _udpClient.ReceiveAsync();

                        Transmission msg = new Transmission(res.Buffer, Transmission.EType.Received);
                        msg.Origin = res.RemoteEndPoint;
                        msg.Destination = _udpClient.Client.LocalEndPoint as IPEndPoint;

                        Received?.Invoke(this, new ReceivedEventArgs(msg));
                    }
                    catch (SocketException ex)
                    {
                        // Ignore this error, triggered after sending 
                        // a packet to an unreachable port. UDP is not
                        // reliable anyway, this can safetly be ignored.
                        if(ex.ErrorCode != 10054)
                        {
                            Stop();
                            break;
                        }
                    }
                    catch(Exception)
                    {                       
                        Stop();
                        break; // end receive;
                    }
                }
            });
        }

        public void Dispose()
        {
            _udpClient?.Close();
            _udpClient = null;
        }
    }

    public class UdpClientServerStatusEventArgs : EventArgs
    {
        public enum EServerStatus { Started, Stopped };

        public EServerStatus ServerStatus { get; private set; }
        public IPEndPoint ServerInfo { get; private set; }

        public UdpClientServerStatusEventArgs(EServerStatus status, IPEndPoint info = null)
        {
            ServerStatus = status;
            ServerInfo = info;
        }
    }

}
