using System;
using System.Collections.Generic;
using System.Text;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Formatter;
using TcpUdpTool.Model.Util;

namespace TcpUdpTool.Model
{
    public class TransmissionHistory
    {
        public event Action HistoryChanged;

        private SortedSet<Piece> _history;
        private StringBuilder _cache;
        private Dictionary<Piece, int> _lengthMap;
        private IFormatter _formatter;
        private int _maxSize = 5;
   
        private object _lock = new object();


        public TransmissionHistory()
        {
            _history = new SortedSet<Piece>();
            _cache = new StringBuilder();
            _lengthMap = new Dictionary<Piece, int>();
        }


        public void SetMaxSize(int size)
        {
            _maxSize = size;
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
                _lengthMap.Clear();
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
            if(_maxSize == 0)
            {
                if (_history.Count > 0) Clear();
                return;
            }

            lock(_lock)
            {
                if(_history.Count > 0 && msg.SequenceNr < _history.Max.SequenceNr)
                {
                    // wrong order, invalidate cache.
                    while(_history.Count >= _maxSize)
                    {
                        _lengthMap.Remove(_history.Min);
                        _history.Remove(_history.Min);
                    }

                    _history.Add(msg);
                    Invalidate();
                }
                else
                {
                    while (_history.Count >= _maxSize)
                    {
                        Piece head = _history.Min;
                        _history.Remove(head);
                        _cache.Remove(0, _lengthMap[head]);
                        _lengthMap.Remove(head);
                    }

                    _history.Add(msg);
                    AppendCache(msg);
                }

                HistoryChanged?.Invoke();
            } 
        }


        private void AppendCache(Piece msg)
        {
            int preLen = _cache.Length;

            _formatter.Format(msg, _cache, SettingsUtils.GetEncoding());

            // save length of formatted message in map.
            _lengthMap.Add(msg, _cache.Length - preLen);
        }

        private void Invalidate()
        {
            _cache.Clear();
            _lengthMap.Clear();
            foreach(var msg in _history)
            {
                AppendCache(msg);
            }
        }

    }
}
