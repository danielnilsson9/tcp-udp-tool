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
            if (_udpClient != null)
                return; // already started.

            _udpClient = new UdpClient(groupIp.AddressFamily);

            _udpClient.Client.SetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));


            if (iface == EMulticastInterface.All)
            {
                NetworkUtils.GetMulticastInterfaces().ForEach(
                    (intrf) =>
                    {
                        // join on all interfaces
                        try
                        {
                            _udpClient.JoinMulticastGroup(groupIp, intrf.IPv4Address);
                        }
                        catch (SocketException)
                        {
                            // ignore, not supported on this interface.
                        }
                    }
                );               
            }
            else if(iface == EMulticastInterface.Specific)
            {
                _udpClient.JoinMulticastGroup(groupIp, specificIface);
            }
            else if(iface == EMulticastInterface.Default)
            {
                _udpClient.JoinMulticastGroup(groupIp);
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

        public async Task<PieceSendResult> SendAsync(Piece msg, IPAddress group, int port, 
            EMulticastInterface minterface, IPAddress specificInterface = null)
        {
            UdpClient sendClient = new UdpClient(group.AddressFamily);

            IPEndPoint from = sendClient.Client.LocalEndPoint as IPEndPoint;
            IPEndPoint to = new IPEndPoint(group, port);

            var interfaces = new List<int>();

            if(minterface == EMulticastInterface.All)
            {
                foreach(var i in NetworkUtils.GetMulticastInterfaces())
                {
                    interfaces.Add(i.Index);
                }
            }
            else if(minterface == EMulticastInterface.Specific)
            {
                int ifindex = NetworkUtils.GetBestMulticastInterfaceIndex(specificInterface);

                if(ifindex != -1)
                {
                    System.Diagnostics.Debug.WriteLine("Sending multicast on specific interface: " + ifindex);
                    interfaces.Add(ifindex);
                }                
            }
                               
            
            if(interfaces.Any())
            {
                // send on specific interfaces.
                foreach (int ifindex in interfaces)
                {
                    sendClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, ifindex);
                    await sendClient.SendAsync(msg.Data, msg.Data.Length, to);
                }
            }
            else
            {
                // send using default interface.
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

                        System.Diagnostics.Debug.WriteLine("Received " + res.Buffer.Length + " bytes from " + msg.Origin.ToString());
                    }
                    catch(Exception e)
                    when(e is ObjectDisposedException || e is SocketException)
                    {
                        Leave();
                        break; // end receive;
                    }                  
                }
            });
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