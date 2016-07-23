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
using TcpUdpTool.Model.EventArg;
using static TcpUdpTool.Model.UdpMulticastClient;

namespace TcpUdpTool.Model
{
    public class UdpMulticastClient
    {

        public enum EMulticastInterface { Default, All, Specific };

        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<UdpMulticastClientStatusEventArgs> StatusChanged;
     
        private UdpClient _udpClient;


        public UdpMulticastClient()
        {

        }


        public void Join(IPAddress groupIp, int port, 
            EMulticastInterface iface, IPAddress specificIface = null)
        {
            Validadate(groupIp, port);

            if (_udpClient != null)
                return; // already started.

            _udpClient = new UdpClient(groupIp.AddressFamily);
            Socket socket = _udpClient.Client;

            socket.SetSocketOption(
                SocketOptionLevel.Socket, 
                SocketOptionName.ReuseAddress, 
                true
            );

            socket.Bind(new IPEndPoint(
                socket.AddressFamily == AddressFamily.InterNetworkV6 ? 
                IPAddress.IPv6Any : 
                IPAddress.Any, port)
            );


            var joinInterfaces = new List<int>();

            if (iface == EMulticastInterface.All)
            {
                foreach (var i in NetworkUtils.GetMulticastInterfaces())
                {
                    joinInterfaces.Add(i.Index);
                }    
            }
            else if(iface == EMulticastInterface.Specific)
            {
                int best = NetworkUtils.GetBestMulticastInterfaceIndex(specificIface);
                if (best == -1) best = 0;
                joinInterfaces.Add(best);
            }
            else if(iface == EMulticastInterface.Default)
            {
                joinInterfaces.Add(0); // 0 = default.
            }

            foreach (int ifaceIndex in joinInterfaces)
            {
                if (socket.AddressFamily == AddressFamily.InterNetwork)
                {
                    MulticastOption opt = new MulticastOption(
                        groupIp, ifaceIndex);

                    socket.SetSocketOption(SocketOptionLevel.IP,
                        SocketOptionName.AddMembership, opt);
                }
                else if (socket.AddressFamily == AddressFamily.InterNetworkV6)
                {                
                    IPv6MulticastOption optv6 = new IPv6MulticastOption(
                        groupIp, ifaceIndex);

                    socket.SetSocketOption(SocketOptionLevel.IPv6,
                        SocketOptionName.AddMembership, optv6);
                }
            }

            StatusChanged?.Invoke(this, 
                new UdpMulticastClientStatusEventArgs(true)
                {
                    MulticastGroup = new IPEndPoint(groupIp, port),
                    MulticastInterface = iface,
                    SpecificInterface = specificIface
                }
            );

            StartReceive();
        }

        public void Leave()
        {
            if(_udpClient != null)
            {
                _udpClient.Close();
                _udpClient = null;
                StatusChanged?.Invoke(this, new UdpMulticastClientStatusEventArgs(false));
            }
        }

        public async Task<PieceSendResult> SendAsync(Piece msg, IPAddress groupIp, int port, 
            EMulticastInterface iface, IPAddress specificIface = null)
        {
            Validadate(groupIp, port);

            UdpClient sendClient = new UdpClient(groupIp.AddressFamily);

            sendClient.Client.Bind(new IPEndPoint(
                sendClient.Client.AddressFamily == AddressFamily.InterNetworkV6 ?
                IPAddress.IPv6Any : IPAddress.Any, 0));


            IPEndPoint from = sendClient.Client.LocalEndPoint as IPEndPoint;
            IPEndPoint to = new IPEndPoint(groupIp, port);
            var sendInterfaces = new List<int>();

            if(iface == EMulticastInterface.All)
            {
                foreach(var i in NetworkUtils.GetMulticastInterfaces())
                {
                    sendInterfaces.Add(i.Index);
                }
            }
            else if(iface == EMulticastInterface.Specific)
            {
                int idx = NetworkUtils.GetBestMulticastInterfaceIndex(specificIface);
                if (idx == -1)
                {
                    idx = 0; // fall back default to default if not found.
                }

                sendInterfaces.Add(idx);
            }
            else
            {
                sendInterfaces.Add(0); // use default interface.
            }


            foreach(var ifaceIndex in sendInterfaces)
            {
                SetSendInterface(sendClient, ifaceIndex);
                await sendClient.SendAsync(msg.Data, msg.Data.Length, to);
            }

            return new PieceSendResult { From = from, To = to };    
        }

        
        private void StartReceive()
        {
            Task.Run(async () =>
            {
                while(_udpClient != null)
                {
                    try
                    {
                        UdpReceiveResult res = await _udpClient.ReceiveAsync();

                        Piece msg = new Piece(res.Buffer, Piece.EType.Received);
                        msg.Origin = res.RemoteEndPoint;
                        msg.Destination = _udpClient.Client.LocalEndPoint as IPEndPoint;

                        Received?.Invoke(this, new ReceivedEventArgs(msg));
                    }
                    catch(SocketException ex)
                    {
                        // Ignore this error, triggered after sending 
                        // a packet to an unreachable port. UDP is not
                        // reliable anyway, this can safetly be ignored.
                        if (ex.ErrorCode != 10054)
                        {
                            Leave();
                            break;
                        }
                    }               
                    catch(Exception)
                    {
                        Leave();
                        break; // end receive;
                    }                  
                }
            });
        }
        
        private void SetSendInterface(UdpClient client, int ifaceIndex)
        {
            // Set the outgoing multicast interface
            Socket socket = client.Client;

            if (socket.AddressFamily == AddressFamily.InterNetwork)
            {
                // Interface index must be in network byte order for IPv4
                // https://msdn.microsoft.com/en-us/library/windows/desktop/ms738586(v=vs.85).aspx

                socket.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.MulticastInterface,
                    IPAddress.HostToNetworkOrder(ifaceIndex)
                );
            }
            else
            {
                // Interface index must be in HOST BYTE ORDER for IPv6
                // Many wasted houers here...
                // https://msdn.microsoft.com/en-us/library/windows/desktop/ms738574(v=vs.85).aspx

                socket.SetSocketOption(
                    SocketOptionLevel.IPv6,
                    SocketOptionName.MulticastInterface,
                    ifaceIndex
                );
            }
        }

        private void Validadate(IPAddress multicastGroup, int port)
        {
            if (!NetworkUtils.IsMulticast(multicastGroup))
            {
                throw new ArgumentException(multicastGroup + " is not a vaild multicast address.");
            }

            if (port < 1 || port > 65535)
            {
                throw new ArgumentException(port + " is not a valid multicast port number.");
            }
        }

    }

    public class UdpMulticastClientStatusEventArgs : EventArgs
    {

        public bool Joined {get; private set;}
        public IPEndPoint MulticastGroup { get; set; }
        public EMulticastInterface MulticastInterface { get; set; }
        public IPAddress SpecificInterface { get; set; }

        public UdpMulticastClientStatusEventArgs(bool joined)
        {
            Joined = joined;
        }

    }

}