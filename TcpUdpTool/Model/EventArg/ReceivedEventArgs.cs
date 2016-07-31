using System;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.EventArg
{
    public class ReceivedEventArgs : EventArgs
    {

        public Piece Message { get; private set; }

        public ReceivedEventArgs(Piece message)
        {
            Message = message;   
        }

    }
}
