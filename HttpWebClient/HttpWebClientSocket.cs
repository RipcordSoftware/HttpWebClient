﻿// The MIT License(MIT)
//
// Copyright(c) 2015-2017 Ripcord Software Ltd
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Net.Sockets;

namespace RipcordSoftware.HttpWebClient
{
    public class HttpWebClientSocket : IHttpWebClientSocket
    {
        #region Types
        protected internal class TcpSocket : IDisposable
        {
            #region Private fields
            private Socket _socket;

            private int _timeout;

            private bool _keepAlive;
            private long _keepAliveStarted;
            private int? _keepAliveTimeout;
            #endregion

            #region Constructor
            public TcpSocket(string hostname, int port, int timeout = 30000)
            {
                Hostname = hostname;
                Port = port;

                _socket = NewSocket(Hostname, Port);
                Timeout = timeout;
                _socket.Connect(hostname, port);

                ResetKeepAlive();
            }
            #endregion

            #region Public methods
            public void Flush()
            {
                if (_socket.NoDelay == false)
                {
                    // setting NoDelay to true will flush the waiting socket data (at least under Mono/Linux)
                    _socket.NoDelay = true;
                    _socket.NoDelay = false;
                }
            }

            public int Receive(byte[] buffer, int offset, int count, bool peek = false, SocketFlags flags = SocketFlags.None)
            {
                flags |= peek ? SocketFlags.Peek : SocketFlags.None;
                return _socket.Receive(buffer, offset, count, flags);
            }

            public int Send(byte[] buffer, int offset, int count, SocketFlags flags = SocketFlags.None)
            {
                return _socket.Send(buffer, offset, count, flags);
            }

            public void Close()
            {
                if (_socket != null)
                {
                    _socket.Disconnect(false);
                    _socket.Close();
                    _socket = null;
                }
            }

            public void KeepAliveOnClose(int? timeout = null)
            {
                _keepAlive = true;
                _keepAliveStarted = Now;
                _keepAliveTimeout = timeout;
            }

            public void ResetKeepAlive()
            {
                _keepAlive = false;
                _keepAliveStarted = 0;
                _keepAliveTimeout = null;
            }
            #endregion

            #region Public properties
            public string Hostname { get; protected set; }
            public int Port { get; protected set; }
            public int Timeout { get { return _timeout; } set { _socket.ReceiveTimeout = value; _socket.SendTimeout = value; _timeout = value; } }

            public bool Connected { get { return _socket.Connected; } }
            public int Available { get { return _socket.Available; } }

            public bool IsKeepAlive { get { return _keepAlive; } }
            public bool IsKeepAliveExpired { get { return _keepAliveTimeout.HasValue ? (_keepAliveStarted + _keepAliveTimeout) < Now : false; } }

            public bool NoDelay { get { return _socket.NoDelay; } set { _socket.NoDelay = value; } }
            #endregion

            #region Private properties
            private static long Now { get { return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; } }
            #endregion

            #region Private methods
            private static Socket NewSocket(string hostname, int port)
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                return socket;
            }
            #endregion

            #region IDisposable implementation
            public void Dispose()
            {
                Close();
            }
            #endregion
        }
        #endregion

        #region Private fields
        private TcpSocket _socket;
        private bool _forceClose = false;
        #endregion

        #region Contructor
        protected HttpWebClientSocket(string hostname, int port, int timeout = 30000)
        {
            _socket = new TcpSocket(hostname, port, timeout);
        }

        protected internal HttpWebClientSocket(HttpWebClientSocket.TcpSocket socket)
        {
            _socket = socket;
        }
        #endregion

        #region Public methods
        public static IHttpWebClientSocket GetSocket(string hostname, int port, int timeout = 30000)
        {
            var socket = HttpWebClientSocketCache.GetSocket(hostname, port, timeout);
            if (socket != null && (socket.IsKeepAliveExpired || !socket.Connected || socket.Available > 0))
            {
                socket.Close();
                socket = null;
            }

            if (socket != null)
            {
                socket.ResetKeepAlive();
                return new HttpWebClientSocket(socket);
            }
            else
            {
                return new HttpWebClientSocket(hostname, port, timeout);
            }
        }

        public void Flush()
        {
            _socket.Flush();
        }

        public int Receive(byte[] buffer, int offset, int count, bool peek = false, SocketFlags flags = SocketFlags.None)
        {
            return _socket.Receive(buffer, offset, count, peek, flags);
        }

        public int Send(byte[] buffer, int offset, int count, SocketFlags flags = SocketFlags.None)
        {
            return _socket.Send(buffer, offset, count, flags);
        }

        public void Close()
        {
            if (_socket != null)
            {
                if (!_forceClose && _socket.IsKeepAlive && !_socket.IsKeepAliveExpired && _socket.Connected && _socket.Available == 0)
                {
                    HttpWebClientSocketCache.ReleaseSocket(_socket);
                }
                else
                {
                    _socket.Close();
                }

                _socket = null;
            }
        }

        public void KeepAliveOnClose(int? timeout = null)
        {
            // if we don't have a keep-alive value then assume half the socket timeout interval
            // TODO: what would be a better value?
            if (!timeout.HasValue)
            {
                timeout = Timeout / 2;
            }                

            _socket.KeepAliveOnClose(timeout);
        }
        #endregion

        #region Public properties
        public bool Connected { get { return _socket.Connected; } }
        public int Available { get { return _socket.Available; } }

        public int Timeout { get { return _socket.Timeout; } set { _socket.Timeout = value; } }

        public bool NoDelay { get { return _socket.NoDelay; } set { _socket.NoDelay = value; } }

        public bool ForceClose { set { _forceClose = value; } }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            Close();
        }
        #endregion
    }
}

