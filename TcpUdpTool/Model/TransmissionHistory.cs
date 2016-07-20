using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Formatter;

namespace TcpUdpTool.Model
{
    public class TransmissionHistory
    {
        public event Action HistoryChanged;

        private List<Piece> _history;
        private StringBuilder _cache;
        private IFormatter _formatter;

        private object _lock = new object();

        public TransmissionHistory()
        {
            _history = new List<Piece>();
            _cache = new StringBuilder();
            _formatter = new PlainTextFormatter(); // default formatter
        }

        public string Get()
        {
            return _cache.ToString();
        }

        public void Clear()
        {
            lock(_lock)
            {
                _history.Clear();
                _cache.Clear();
                HistoryChanged?.Invoke();
            }           
        }

        public void SetFormatter(IFormatter formatter)
        {
            lock(_lock)
            {
                _formatter = formatter;
                Invalidate();
                HistoryChanged?.Invoke();
            }
            
        }

        public void Append(Piece msg)
        {
            lock(_lock)
            {
                _history.Add(msg);
                AppendCache(msg);
                HistoryChanged?.Invoke();
            } 
        }


        private void AppendCache(Piece msg)
        {
            if (_cache.Length != 0)
            {
                _cache.AppendLine();
            }

            _formatter.Format(msg, _cache);     
        }

        private void Invalidate()
        {
            _cache.Clear();
            foreach(var msg in _history)
            {
                AppendCache(msg);
            }
        }

    }
}
