using System.Text;

namespace TcpUdpTool.Model.Parser
{
    public interface IParser
    {
        byte[] Parse(string text, Encoding encoding = null);
    }
}
