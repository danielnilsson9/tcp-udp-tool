using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UdpTcpTool.Model;

namespace UdpTcpTool.Impl
{
    public class MyTcpClient
    {
        public event Action<Piece> DataReceived;
        public event Action<bool> ConnectStatusChanged;

        private TcpClient _tcpClient;
        private byte[] _buffer;

        
        public MyTcpClient()
        {
            _tcpClient = new TcpClient();
            _buffer = new byte[8192];            
        }


        public void Connect(string host, int port)
        {
            if (_tcpClient.Connected)
                return; // already connected

            _tcpClient.BeginConnect(host, port, new AsyncCallback((ar) =>
                {
                    try
                    {
                        _tcpClient.EndConnect(ar);

                        ConnectStatusChanged?.Invoke(true);

                        Receive();
                    }
                    catch(ObjectDisposedException)
                    {
                        // ignore, disconnected.
                    }
                }
            ), null);
        }

        public void Disconnect()
        {
            _tcpClient.Close();
            ConnectStatusChanged?.Invoke(false);
        }

        public void Send(Piece msg)
        {
            if(!_tcpClient.Connected)
            {
                return;
            }

            _tcpClient.GetStream().WriteAsync(msg.Data, 0, msg.Length);
        }

      
        private void Receive()
        {
            if (!_tcpClient.Connected)
                return;

            _tcpClient.GetStream().BeginRead(_buffer, 0, _buffer.Length, new AsyncCallback((ar) =>
                {
                    try
                    {
                        int read = _tcpClient.GetStream().EndRead(ar);
                        if(read > 0)
                        {
                            byte[] data = new byte[read];
                            Array.Copy(_buffer, data, read);

                            DataReceived?.Invoke(new Piece(data, Piece.EType.Received));

                            // read again
                            Receive();
                        }
                        else
                        {
                            // disconnect, server closed connection.
                            ConnectStatusChanged?.Invoke(false);
                        }
                    }
                    catch(Exception e) 
                    when (e is ObjectDisposedException || e is IOException) 
                    {
                        // disconnected
                        if (_tcpClient.Connected)
                        {
                            ConnectStatusChanged?.Invoke(false);
                        }
                    }                 
                }
            ), null);       
        }

    }
}
