using System;
using System.Net;
using System.Net.Sockets;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Base;

namespace TcpUdpTool.ViewModel.Item
{
    public class InterfaceAddress : ObservableObject, IComparable
    {
        public enum EInterfaceType { Default, All, Any, Specific }


        private EInterfaceType _type;
        public EInterfaceType Type
        {
            get { return _type; }
            private set { _type = value; }
        }

        private IPAddress _address;
        public IPAddress Address
        {
            get { return _address; }
            private set { _address = value; }
        }

        public string Name
        {
            get { return ToString(); }
        }

        public NetworkInterface Nic { get; private set; }

        public string GroupName
        {
            get { return Nic == null ? "Network Interface" : Nic.Name; }
        }


        public InterfaceAddress(EInterfaceType type, NetworkInterface nic, IPAddress address = null)
        {
            Type = type;
            Address = address;
            Nic = nic;

            if(Address == null && (Type == EInterfaceType.Any || Type == EInterfaceType.Specific))
            {
                throw new ArgumentNullException(
                    "address cannot be null for types: [Specific, Any]");
            }
        }

        public override string ToString()
        {
            if (Type == EInterfaceType.Default)
            {
                return "Default";
            }
            else if (Type == EInterfaceType.All)
            {
                return "All";
            }
            else if (Type == EInterfaceType.Any)
            {
                if (Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return "Any IPv4 (0.0.0.0)";
                }
                else
                {
                    return "Any IPv6 (::)";
                }
            }
            else
            {
                return Address.ToString();
            }
        }

        public int CompareTo(object other)
        {
            InterfaceAddress o = other as InterfaceAddress;

            if (o == null)
            {
                return 0;
            }
                
            int r = this.Type.CompareTo(o.Type);

            if(r == 0)
            {
                return this.ToString().CompareTo(o.ToString());
            }             
            else
            {
                return r;
            }               
        }
    }
}
