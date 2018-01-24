using System;
using System.Net;

namespace TcpUdpTool.Model.Data
{
    public class Transmission : IEquatable<Transmission>, IComparable<Transmission>
    {
        private static ulong _counter = 0;
        private static object _lock = new object();


        public enum EType { Sent, Received };

        private ulong _sequenceNr;
        private byte[] _data;
        private EType _type;
        private DateTime _timestamp;
        private IPEndPoint _origin;
        private IPEndPoint _destination;


        public ulong SequenceNr
        {
            get { return _sequenceNr; }
        }

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


        public Transmission(byte[] data, EType type)
        {
            lock(_lock)
            {
                _sequenceNr = _counter++;
            }

            _data = data;
            _type = type;
            _timestamp = DateTime.Now;
        }

        public bool Equals(Transmission other)
        {
            if(other != null)
            {
                return SequenceNr == other.SequenceNr;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Transmission);
        }

        public override int GetHashCode()
        {
            return (int)SequenceNr;
        }

        public int CompareTo(Transmission other)
        {
            if(SequenceNr > other.SequenceNr)
            {
                return 1;
            }
            else if(SequenceNr < other.SequenceNr)
            {
                return -1;
            }

            return 0;
        }
    }
}
