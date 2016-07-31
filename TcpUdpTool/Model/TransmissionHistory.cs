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

        private LinkedList<Piece> _history;
        private StringBuilder _cache;
        private Dictionary<Piece, int> _lengthMap;
        private IFormatter _formatter;
        private int _maxSize = 5;
   
        private object _lock = new object();


        public TransmissionHistory()
        {
            _history = new LinkedList<Piece>();
            _cache = new StringBuilder();
            _formatter = new PlainTextFormatter(); // default formatter
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
               
                if(_history.Count > 0 && msg.Timestamp < _history.Last.Value.Timestamp)
                {
                    // remove if history larger than max size
                    while (_history.Count >= _maxSize)
                    {
                        _lengthMap.Remove(_history.First.Value);
                        _history.RemoveFirst();                     
                    }

                    // wrong order, insert before last
                    _history.AddBefore(_history.Last, msg);

                    Invalidate();                       
                }
                else
                {
                    // remove if history larger than max size
                    while (_history.Count >= _maxSize)
                    {
                        Piece head = _history.First.Value;
                        _history.RemoveFirst();
                        _cache.Remove(0, _lengthMap[head]);
                        _lengthMap.Remove(head);
                    }


                    _history.AddLast(msg);
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
