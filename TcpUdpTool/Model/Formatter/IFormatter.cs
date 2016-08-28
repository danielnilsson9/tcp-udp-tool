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
        void SetPrintIP(bool printIp);

        void SetPrintTime(bool printTime);

        void Format(Piece data, StringBuilder builder, Encoding encoding = null);
    }
}
