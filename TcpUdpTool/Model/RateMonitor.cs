using System;
using System.Diagnostics;
using System.Threading;
using TcpUdpTool.Model.Data;

namespace TcpUdpTool.Model
{
    public class RateMonitor : IDisposable
    {

        #region private members

        private ulong _recevedBytes;
        private ulong _sentBytes;

        private ulong _recvSinceLastUpdate;
        private ulong _sentSinceLastUpdate;

        private Timer _updateTimer;
        private Stopwatch _stopwatch;

        #endregion

        #region public properties

        public ulong TotalSentBytes { get; set; }
        public ulong TotalReceivedBytes { get; set; }
        public ulong CurrentSendRate { get; set; }
        public ulong CurrentReceiveRate { get; set; }

        #endregion

        #region constructors

        public RateMonitor()
        {

        }

        #endregion

        #region public functions 

        public void Start()
        {
            if(_updateTimer == null)
            {
                Reset();
                _stopwatch = new Stopwatch();
                _updateTimer = new Timer(OnUpdate, null, 0, 500);
            }
        }

        public void Stop()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Dispose();
                _updateTimer = null;
            }
            Reset();
        }

        public void Reset()
        {
            lock(this)
            {
                _recevedBytes = 0;
                _sentBytes = 0;
                _recvSinceLastUpdate = 0;
                _sentSinceLastUpdate = 0;
                TotalSentBytes = 0;
                TotalReceivedBytes = 0;
                CurrentReceiveRate = 0;
                CurrentSendRate = 0;
            }       
        }

        public void NoteReceived(Transmission data)
        {
            lock(this)
            {
                _recevedBytes += (uint)data.Length;
                _recvSinceLastUpdate+= (uint)data.Length;
            }
        }

        public void NoteSent(Transmission data)
        {
            lock(this)
            {
                _sentBytes += (uint)data.Length;
                _sentSinceLastUpdate += (uint)data.Length;
            }
        }

        #endregion

        #region private functions
  
        private void OnUpdate(object state)
        {
            lock (this)
            {
                _stopwatch.Stop();
                ulong millis = (ulong)_stopwatch.ElapsedMilliseconds;             
                if (millis > 0)
                {
                    TotalSentBytes = _sentBytes;
                    TotalReceivedBytes = _recevedBytes;
                    CurrentSendRate = (_sentSinceLastUpdate * 8 * 1000) / millis;
                    CurrentReceiveRate = (_recvSinceLastUpdate * 8 * 1000) / millis;
                    _sentSinceLastUpdate = 0;
                    _recvSinceLastUpdate = 0;
                }
                _stopwatch.Reset();
                _stopwatch.Start();
            }
        }

        public void Dispose()
        {
            _updateTimer?.Dispose();
        }

        #endregion

    }
}
