using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpUdpTool.Model.Util
{
    public static class InterfaceUtils
    {

        public static List<IPAddress> GetAllInterfaces()
        {
            List<IPAddress> result = new List<IPAddress>();

            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (!iface.Supports(NetworkInterfaceComponent.IPv4) || 
                    iface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                IPInterfaceProperties ap = iface.GetIPProperties();
                UnicastIPAddressInformationCollection uips = ap.UnicastAddresses;

                foreach (var uip in uips)
                {
                    if (uip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        result.Add(uip.Address);
                    }
                }
            }

            return result;
        }


    }
}
