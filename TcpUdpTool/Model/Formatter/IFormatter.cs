using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public interface IFormatter
    {
        string Format(Piece data);
    }
}
