using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Formatter;

namespace TcpUdpTool.Model
{
    class TransmissionHistory
    {
        public event Action HistoryChanged;

        private List<Piece> _history;
        private StringBuilder _cache;
        private IFormatter _formatter;

        public TransmissionHistory()
        {
            _history = new List<Piece>();
            _cache = new StringBuilder();
        }

        public string Get()
        {
            return _cache.ToString();
        }

        public void Clear()
        {
            _history.Clear();
            _cache.Clear();
            HistoryChanged?.Invoke();
        }

        public void SetFormatter(IFormatter formatter)
        {
            _formatter = formatter;
            Invalidate();
            HistoryChanged?.Invoke();
        }

        public void Append(Piece msg)
        {
            _history.Add(msg);
            AppendCache(msg);
            HistoryChanged?.Invoke();
        }



        private void AppendCache(Piece msg)
        {
            if (_cache.Length != 0)
            {
                _cache.AppendLine();
            }
        
            _cache.AppendFormat("[{0}]{1}: ", msg.Timestamp.ToString("HH:mm:ss"), msg.IsSent ? "S" : "R");
            _cache.Append(Encoding.UTF8.GetString(msg.Data));       
        }

        private void Invalidate()
        {

        }

    }
}
