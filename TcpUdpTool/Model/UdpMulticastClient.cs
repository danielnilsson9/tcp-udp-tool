using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Util;
using TcpUdpTool.Model.EventArg;
using static TcpUdpTool.Model.UdpMulticastClient;

namespace TcpUdpTool.Model
{

    public class UdpMulticastClient : IDisposable
    {

        public enum EMulticastInterface { Default, All, Specific };

        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<UdpMulticastClientStatusEventArgs> StatusChanged;
     
        private UdpClient _udpClient;
        private UdpClient _sendUdpClient;


        public UdpMulticastClient()
        {
        }

        public void JoinASM(IPAddress groupIp, int port, 
            EMulticastInterface iface, IPAddress specificIface = null)
        {
            Validate(groupIp, port);

            if (_udpClient != null)
                return; // already started.

            _udpClient = new UdpClient(groupIp.AddressFamily);
            Socket socket = _udpClient.Client;

            socket.SetSocketOption(
                SocketOptionLevel.Socket, 
                SocketOptionName.ReuseAddress, 
                true
            );

            var bindInterface = specificIface;
            if (iface != EMulticastInterface.Specific)
            {
                bindInterface = (socket.AddressFamily == AddressFamily.InterNetworkV6 ? 
                    IPAddress.IPv6Any : IPAddress.Any);
            }
            socket.Bind(new IPEndPoint(bindInterface, port));

            var joinInterfaces = new List<int>();
            if (iface == EMulticastInterface.All)
            {
                foreach (var ni in NetworkUtils.GetActiveInterfaces())
                {
                    joinInterfaces.Add(socket.AddressFamily == AddressFamily.InterNetworkV6 
                        ? ni.IPv6.Index : ni.IPv4.Index);
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

        public void JoinSSM(IPAddress groupIp, IPAddress sourceIp, int port, 
            EMulticastInterface iface, IPAddress specificIface = null)
        {
            Validate(groupIp, port);

            if (_udpClient != null)
                return; // already started.

            _udpClient = new UdpClient(groupIp.AddressFamily);
            Socket socket = _udpClient.Client;

            socket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true
            );

            var bindInterface = specificIface;
            if (iface != EMulticastInterface.Specific)
            {
                bindInterface = (socket.AddressFamily == AddressFamily.InterNetworkV6 ?
                    IPAddress.IPv6Any : IPAddress.Any);
            }
            socket.Bind(new IPEndPoint(bindInterface, port));

            var joinInterfaces = new List<int>();
            if (iface == EMulticastInterface.All)
            {
                foreach (var ni in NetworkUtils.GetActiveInterfaces())
                {
                    joinInterfaces.Add(socket.AddressFamily == AddressFamily.InterNetworkV6
                        ? ni.IPv6.Index : ni.IPv4.Index);
                }
            }
            else if (iface == EMulticastInterface.Specific)
            {
                int best = NetworkUtils.GetBestMulticastInterfaceIndex(specificIface);
                if (best == -1) best = 0;
                joinInterfaces.Add(best);
            }
            else if (iface == EMulticastInterface.Default)
            {
                joinInterfaces.Add(0); // 0 = default.
            }

            foreach (int ifaceIndex in joinInterfaces)
            {
                if (socket.AddressFamily == AddressFamily.InterNetwork)
                {
                    var bin = new byte[12];
                    Buffer.BlockCopy(groupIp.GetAddressBytes(), 0, bin, 0, 4);
                    Buffer.BlockCopy(sourceIp.GetAddressBytes(), 0, bin, 4, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(ifaceIndex)), 0, bin, 8, 4);

                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddSourceMembership, bin);
                }
                else if (socket.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // fill data as in GROUP_SOURCE_REQ struct
                    var bin = CreateGroupSourceReg(sourceIp, groupIp, ifaceIndex);

                    socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)45, bin);
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

        public async Task<TransmissionResult> SendAsync(Transmission msg, IPAddress groupIp, int port, 
            EMulticastInterface iface, IPAddress specificIface = null, int ttl = 1)
        {
            Validate(groupIp, port);

            if (_sendUdpClient == null || _sendUdpClient.Client.AddressFamily != groupIp.AddressFamily)
            {
                if (_sendUdpClient != null)
                    _sendUdpClient.Close();

                _sendUdpClient = new UdpClient(groupIp.AddressFamily);
                _sendUdpClient.Client.Bind(new IPEndPoint(
                    _sendUdpClient.Client.AddressFamily == AddressFamily.InterNetworkV6 ?
                    IPAddress.IPv6Any : IPAddress.Any, 0));
            }

            SetSendTTL(_sendUdpClient, ttl);

            IPEndPoint from = _sendUdpClient.Client.LocalEndPoint as IPEndPoint;
            IPEndPoint to = new IPEndPoint(groupIp, port);
            var sendInterfaces = new List<int>();

            if(iface == EMulticastInterface.All)
            {
                foreach(var ni in NetworkUtils.GetActiveInterfaces())
                {
                    sendInterfaces.Add(_sendUdpClient.Client.AddressFamily == AddressFamily.InterNetworkV6 
                        ? ni.IPv6.Index : ni.IPv4.Index);
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
                SetSendInterface(_sendUdpClient, ifaceIndex);
                await _sendUdpClient.SendAsync(msg.Data, msg.Data.Length, to);
            }

            return new TransmissionResult { From = from, To = to };    
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

                        Transmission msg = new Transmission(res.Buffer, Transmission.EType.Received);
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
            var socket = client.Client;

            if (socket.AddressFamily == AddressFamily.InterNetwork)
            {
                // Interface index must be in NETWORK BYTE ORDER for IPv4
                // https://msdn.microsoft.com/en-us/library/windows/desktop/ms738586(v=vs.85).aspx

                socket.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.MulticastInterface,
                    IPAddress.HostToNetworkOrder(ifaceIndex)
                );
            }
            else if(socket.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // Interface index must be in HOST BYTE ORDER for IPv6
                // https://msdn.microsoft.com/en-us/library/windows/desktop/ms738574(v=vs.85).aspx

                socket.SetSocketOption(
                    SocketOptionLevel.IPv6,
                    SocketOptionName.MulticastInterface,
                    ifaceIndex
                );
            }
        }

        private void SetSendTTL(UdpClient client, int ttl)
        {
            if(ttl < 1)
            {
                ttl = 1;
            }
            else if(ttl > 255)
            {
                ttl = 255;
            }

            var socket = client.Client;

            if(socket.AddressFamily == AddressFamily.InterNetwork)
            {
                socket.SetSocketOption(
                    SocketOptionLevel.IP, 
                    SocketOptionName.MulticastTimeToLive, 
                    ttl
                );
            }
            else if(socket.AddressFamily == AddressFamily.InterNetworkV6)
            {
                socket.SetSocketOption(
                    SocketOptionLevel.IPv6, 
                    SocketOptionName.MulticastTimeToLive, 
                    ttl
                );
            }
        }

        private void Validate(IPAddress multicastGroup, int port)
        {
            if (!NetworkUtils.IsMulticast(multicastGroup))
            {
                throw new ArgumentException(multicastGroup + " is not a vaild multicast address.");
            }

            if (!NetworkUtils.IsValidPort(port, false))
            {
                throw new ArgumentException(port + " is not a valid multicast port number.");
            }
        }

        public void Dispose()
        {
            _udpClient?.Close();
            _udpClient = null;
            _sendUdpClient?.Close();
            _sendUdpClient = null;
        }


        private static byte[] CreateGroupSourceReg(IPAddress source, IPAddress group, int ifaceIndex)
        {
            /*
             * struct group_source_reg
             * {
             *    ULONG              gsr_interface;     4 + (4 padding after)
             *    SOCKADDR_STORAGE   gsr_group;         128
             *    SOCKADDR_STORAGE   gsr_source;        128
             * }
             */
            var bin = new byte[264];

            int offset = 0;
            Buffer.BlockCopy(BitConverter.GetBytes((ulong)ifaceIndex), 0, bin, offset, 4);
            offset += 4;
            offset += 4; // skip padding
            FillSockAddrStorage(bin, offset, group);
            offset += 128;
            FillSockAddrStorage(bin, offset, source);

            return bin;
        }

        private static void FillSockAddrStorage(byte[] dst, int offset, IPAddress address)
        {
            /*
             * struct sockaddr_storage
             * {
             *     short ss_family;
             *     char pad1[6];
             *     int64 align;
             *     char pad2[112]
             * }
             * 
             * struct sockaddr_in
             * {
             *     short sin_familiy;
             *     ushort sin_port;
             *     byte[4] sin_addr;
             *     char sin_zero[8]
             * }
             * 
             * struct sockaddr_in6
             * {
             *     short sin6_family;
             *     ushort sin6_port;
             *     ulong sin6_flowinfo;
             *     byte[16] sin6_addr
             *     ulong sin6_scope_id;
             * }
             * 
             */
            Buffer.BlockCopy(BitConverter.GetBytes((short)AddressFamily.InterNetworkV6), 0, dst, offset, 2);
            offset += 2;

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                offset += 2; // skip port, 2 bytes
                Buffer.BlockCopy(address.GetAddressBytes(), 0, dst, offset, 4);
                offset += 8; // skip zeros, 8 bytes
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                offset += 2; // skip port, 2 bytes
                offset += 4; // skip flowinfo, 4 bytes
                Buffer.BlockCopy(address.GetAddressBytes(), 0, dst, offset, 16);
                offset += 4; // skip scope id, 4 bytes
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