using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpUdpTool.Model.Util
{

    public static class NetworkUtils
    {

        public static event Action NetworkInterfaceChange;

        static NetworkUtils()
        {
            NetworkChange.NetworkAddressChanged += (s, e) => NetworkInterfaceChange?.Invoke();
            NetworkChange.NetworkAvailabilityChanged += (s, e) => NetworkInterfaceChange?.Invoke();
        }

        public static List<NetworkInterface> GetActiveInterfaces()
        {
            var result = new List<NetworkInterface>();

            foreach (var adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((!adapter.Supports(NetworkInterfaceComponent.IPv4) && 
                    !adapter.Supports(NetworkInterfaceComponent.IPv6)) ||
                    adapter.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }
   
                var ni = new NetworkInterface();
                ni.Id = adapter.Id;
                ni.Name = adapter.Name;
                ni.Description = adapter.Description;

                var aip = adapter.GetIPProperties();

                if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    ni.IPv4.Index = aip.GetIPv4Properties().Index;
                }

                if (adapter.Supports(NetworkInterfaceComponent.IPv6))
                {
                    ni.IPv6.Index = aip.GetIPv6Properties().Index;
                }

                foreach (var uip in aip.UnicastAddresses)
                {
                    if (uip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ni.IPv4.Addresses.Add(uip.Address);
                    }
                    else if (uip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        ni.IPv6.Addresses.Add(uip.Address);
                    }
                }

                result.Add(ni);
            }

            return result;
        }

        public static int GetBestMulticastInterfaceIndex(IPAddress localInterface)
        {
            var interfaces = GetActiveInterfaces();
            foreach(var intf in interfaces)
            {
                if (localInterface.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (intf.IPv4.Addresses.Contains(localInterface))
                    {
                        return intf.IPv4.Index;
                    }
                }
                else if (localInterface.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (intf.IPv6.Addresses.Contains(localInterface))
                    {
                        return intf.IPv6.Index;
                    }
                }
            }
           
            return -1;
        }

        public static bool IsMulticast(IPAddress ipAddress)
        {
            bool isMulticast = false;

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // In IPv6 Multicast addresses first byte is 0xFF
                byte[] bytes = ipAddress.GetAddressBytes();
                isMulticast = (bytes[0] == 0xff);
            }
            else if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                // In IPv4 Multicast addresses first byte is between 224 and 239
                byte[] bytes = ipAddress.GetAddressBytes();
                isMulticast = (bytes[0] >= 224) && (bytes[0] <= 239);
            }

            return isMulticast;
        }

        public static bool IsSourceSpecificMulticast(IPAddress ipAddress)
        {
            bool isSSM = false;

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // In IPv6 SSM first byte is 0xFF and second byte is 0x3X
                byte[] bytes = ipAddress.GetAddressBytes();
                isSSM = (bytes[0] == 0xff && (bytes[1] >> 4) == 0x03);
            }
            else if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                // In IPv4 SSM first byte is 232
                byte[] addressBytes = ipAddress.GetAddressBytes();
                isSSM = addressBytes[0] == 232;
            }

            return isSSM;
        }

        public static bool IsValidPort(int port, bool allowZero = false)
        {
            return (port >= (allowZero ? 0 : 1)) && port < 65536;
        }

        public static async Task<IPAddress> DnsResolveAsync(string hostOrAddress, bool favorIpV6 = false)
        {
            var favoredFamily = favorIpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            var addrs = await Dns.GetHostAddressesAsync(hostOrAddress);
            return addrs.FirstOrDefault(addr => addr.AddressFamily == favoredFamily)
                 ?? addrs.FirstOrDefault();
        }
  
    }

    public class NetworkInterface
    {
        public class IPInterface
        {
            public IPInterface()
            {
                Index = -1;
                Addresses = new List<IPAddress>();
            }

            public int Index { get; set; }
            public List<IPAddress> Addresses { get; }
        }

        public NetworkInterface()
        {
            IPv4 = new IPInterface();
            IPv6 = new IPInterface();
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IPInterface IPv4 { get; private set; }

        public IPInterface IPv6 { get; private set; }

    }

}
