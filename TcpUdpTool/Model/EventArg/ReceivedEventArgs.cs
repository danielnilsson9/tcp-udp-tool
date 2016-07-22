using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.EventArgs
{
    public class ReceivedEventArgs : System.EventArgs
    {

        public Piece Message { get; private set; }

        public ReceivedEventArgs(Piece message)
        {
            Message = message;   
        }

    }
}
