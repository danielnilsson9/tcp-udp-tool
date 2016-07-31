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

        public static List<LocalInterface> GetActiveInterfaces()
        {
            var result = new List<LocalInterface>();

            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!adapter.Supports(NetworkInterfaceComponent.IPv4) ||
                    adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                IPInterfaceProperties aip = adapter.GetIPProperties();
                IPv4InterfaceProperties ipv4p = aip.GetIPv4Properties();

                LocalInterface li = new LocalInterface(ipv4p.Index);
                UnicastIPAddressInformationCollection uips = aip.UnicastAddresses;

                foreach (var uip in uips)
                {
                    if (uip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        li.IPv4Address = uip.Address;
                    }
                    else if (uip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        li.IPv6Address = uip.Address;
                    }
                }

                result.Add(li);
            }

            return result;
        }

        public static List<LocalInterface> GetMulticastInterfaces()
        {
            var result = new List<LocalInterface>();

            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!adapter.Supports(NetworkInterfaceComponent.IPv4) || 
                    adapter.OperationalStatus != OperationalStatus.Up)               
                    continue;

                if (!adapter.SupportsMulticast)
                    continue;

                IPInterfaceProperties aip = adapter.GetIPProperties();
                IPv4InterfaceProperties ipv4p = aip.GetIPv4Properties();

                if (!aip.MulticastAddresses.Any())
                    continue;

                LocalInterface li = new LocalInterface(ipv4p.Index);
                UnicastIPAddressInformationCollection uips = aip.UnicastAddresses;

                foreach (var uip in uips)
                {
                    if (uip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        li.IPv4Address = uip.Address;
                    }
                    else if(uip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        li.IPv6Address = uip.Address;
                    }
                }

                result.Add(li);
            }

            return result;
        }

        public static int GetBestMulticastInterfaceIndex(IPAddress localInterface)
        {
            var interfaces = GetMulticastInterfaces();

            foreach(var intf in interfaces)
            {
                if(intf.IPv4Address.Equals(localInterface) || intf.IPv6Address.Equals(localInterface))
                {
                    return intf.Index;
                }
            }
           
            return -1;
        }

        public static bool IsMulticast(IPAddress ipAddress)
        {
            bool isMulticast;

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // In IPv6 Multicast addresses first byte is 0xFF
                byte[] ipv6Bytes = ipAddress.GetAddressBytes();
                isMulticast = (ipv6Bytes[0] == 0xff);
            }
            else // IPv4
            {
                // In IPv4 Multicast addresses first byte is between 224 and 239
                byte[] addressBytes = ipAddress.GetAddressBytes();
                isMulticast = (addressBytes[0] >= 224) && (addressBytes[0] <= 239);
            }

            return isMulticast;
        }


        public static async Task<IPAddress> DnsResolveAsync(string hostOrAddress, bool favorIpV6 = false)
        {
            var favoredFamily = favorIpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            var addrs = await Dns.GetHostAddressesAsync(hostOrAddress);
            return addrs.FirstOrDefault(addr => addr.AddressFamily == favoredFamily)
                 ?? addrs.FirstOrDefault();
        }
  
    }

    public class LocalInterface
    {
        public int Index { get; set; }
        public IPAddress IPv4Address { get; set; }
        public IPAddress IPv6Address { get; set; }

        public LocalInterface(int index)
        {
            Index = index;
        }
    }

}
