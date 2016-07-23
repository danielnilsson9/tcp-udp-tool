using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Parser
{
    public interface IParser
    {
        void SetEncoding(Encoding encoding);
        byte[] Parse(string text);
    }
}
