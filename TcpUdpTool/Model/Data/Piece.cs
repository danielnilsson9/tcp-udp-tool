using System;
using System.Net;

namespace TcpUdpTool.Model.Data
{
    public class Piece
    {
        public enum EType { Sent, Received };

        private byte[] _data;
        private EType _type;
        private DateTime _timestamp;
        private IPEndPoint _origin;
        private IPEndPoint _destination;

        public byte[] Data
        {
            get { return _data; }
        }

        public EType Type
        {
            get { return _type; }
        }

        public IPEndPoint Origin
        {
            get { return _origin; }
            set{ _origin = value; }
        }

        public IPEndPoint Destination
        {
            get { return _destination; }
            set { _destination = value; }
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
