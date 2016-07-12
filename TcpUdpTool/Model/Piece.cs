using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpTcpTool.Model
{
    public class Piece
    {
        public enum EType { Sent, Received };

        private byte[] _data;
        private EType _type;
        private DateTime _timestamp;


        public byte[] Data
        {
            get { return _data; }
        }

        public EType Type
        {
            get { return _type; }
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
        }

        public bool IsReceived
        {
            get { return _type == EType.Received; }
        }

        public bool IsSent
        {
            get { return _type == EType.Sent; }
        }
            
        public int Length
        {
            get { return _data.Length; }
        }


        public Piece(byte[] data, EType type)
        {
            _data = data;
            _type = type;
            _timestamp = DateTime.Now;
        }

    }
}
