using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TcpUdpTool.Model.Data
{
    public class PieceSendResult
    {
        public IPEndPoint From { get; set; }
        public IPEndPoint To { get; set; }

    }
}
