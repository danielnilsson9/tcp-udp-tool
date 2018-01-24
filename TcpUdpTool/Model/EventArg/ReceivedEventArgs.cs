using System;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.EventArg
{
    public class ReceivedEventArgs : EventArgs
    {

        public Transmission Message { get; private set; }

        public ReceivedEventArgs(Transmission message)
        {
            Message = message;   
        }

    }
}
