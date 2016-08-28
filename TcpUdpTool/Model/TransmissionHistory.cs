using System;
using System.Collections.Generic;
using System.Windows.Documents;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Formatter;
using TcpUdpTool.Model.Util;

namespace TcpUdpTool.Model
{
    public class TransmissionHistory
    {
        public event Action HistoryChanged;

        private SortedSet<Piece> _history;
        private FlowDocument _target;
        private IFormatter _formatter;
        private int _maxSize = 100;
   
        private object _lock = new object();


        public TransmissionHistory(FlowDocument target)
        {
            _history = new SortedSet<Piece>();
            _target = target;
        }


        public void SetMaxSize(int size)
        {
            _maxSize = size;
        }

        public void Clear()
        {
            lock(_lock)
            {
                _history.Clear();
                _target.Blocks.Clear();
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
                        _target.Blocks.Remove(_target.Blocks.FirstBlock);
                    }

                    _history.Add(msg);
                    AppendCache(msg);
                }

                HistoryChanged?.Invoke();
            } 
        }


        private void AppendCache(Piece msg)
        {
            var item = _formatter.Format(msg, SettingsUtils.GetEncoding());
            _target.Blocks.Add(item);
        }

        private void Invalidate()
        {
            _target.Blocks.Clear();
            foreach(var msg in _history)
            {
                AppendCache(msg);
            }
        }

    }
}
