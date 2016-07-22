using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel.Item
{
    public class InterfaceItem : ObservableObject, IComparable
    {
        public enum EInterfaceType { Default, All, Any, Specific }


        private EInterfaceType _type;
        public EInterfaceType Type
        {
            get { return _type; }
            private set { _type = value; }
        }


        private IPAddress _interface;
        public IPAddress Interface
        {
            get { return _interface; }
            private set { _interface = value; }
        }


        public InterfaceItem(EInterfaceType type, IPAddress localInterface = null)
        {
            Type = type;
            Interface = localInterface;

            if(Interface == null && 
                (Type == EInterfaceType.Any || Type == EInterfaceType.Specific))
            {
                throw new ArgumentNullException(
                    "localInterface cannot be null for types: [Specific, Any]");
            }

            if(localInterface != null)
            {
                if(localInterface.AddressFamily != AddressFamily.InterNetwork && 
                   localInterface.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    throw new ArgumentException("localInterface is of unsupported AdressFamily.");
                }
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
                if (Interface.AddressFamily == AddressFamily.InterNetwork)
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
                return Interface.ToString();
            }
        }

        public int CompareTo(object other)
        {
            InterfaceItem o = other as InterfaceItem;

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
