using System.Text;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model.Formatter
{
    public interface IFormatter
    {
        string Format(Transmission msg);
    }
}
