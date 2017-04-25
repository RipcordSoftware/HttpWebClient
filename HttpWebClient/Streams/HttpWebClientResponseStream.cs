// The MIT License(MIT)
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
using System.IO;

using Ionic.Zlib;

namespace RipcordSoftware.HttpWebClient
{
    internal sealed class HttpWebClientResponseStream : Stream, IHttpWebClientResponseStream
    {
        #region Private fields
        private IHttpWebClientSocket _socket;

        private long _position = 0;
        private long? _length;

        private MemoryStream _memStream;
        #endregion

        #region Constructor
        public HttpWebClientResponseStream(IHttpWebClientSocket socket, MemoryStream memStream, long? length = null)
        {
            _socket = socket;
            _memStream = memStream;
            _length = length;
        }
        #endregion

        #region implemented abstract members of Stream
        public override void Close()
        {
            if (_socket != null)
            {
                Flush();
                _socket.Close();
                _socket = null;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer, offset, count, false);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        public override long Length { get { return _length.HasValue ? _length.Value : _position; } }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region Public methods
        public int Read(byte[] buffer, int offset, int count, bool peek)
        {
            int size = 0;
            long wantLength = _length.HasValue ? _length.Value : long.MaxValue;

            if (_memStream != null && _position < wantLength)
            {
                var startPos = _memStream.Position;
                _memStream.Read(buffer, offset, count);
                size = (int)(_memStream.Position - startPos);

                if (peek)
                {
                    _memStream.Position = startPos;
                }
                else
                {
                    if (_memStream.Position == _memStream.Length)
                    {
                        _memStream = null;
                    }

                    _position += size;
                }
                
                offset += size;
                count -= size;
            }

            if (_socket != null && (size == 0 || _socket.Available > 0) && count > 0 && _position < wantLength)
            {
                var received = _socket.Receive(buffer, offset, count, peek);
                size += received;

                if (!peek)
                {
                    _position += received;
                }
            }

            return size;
        }
        #endregion

        #region IHttpWebClientResponseStream implementation
        public int Available { get { return BufferAvailable + SocketAvailable; } }

        public int SocketAvailable { get { return _socket != null ? _socket.Available : 0; } }

        public int BufferAvailable { get { return _memStream != null ? (int)(_memStream.Length - _memStream.Position) : 0; } }

        public bool SocketForceClose { set { if (_socket != null) { _socket.ForceClose = value; } }
        }
            
        public int SocketReceive(byte[] buffer, int offset, int count)
        {
            return _socket != null ? _socket.Receive(buffer, offset, count) : 0;
        }
        #endregion
    }
}
