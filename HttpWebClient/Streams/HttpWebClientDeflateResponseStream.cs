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
    internal sealed class HttpWebClientDeflateResponseStream : Stream, IHttpWebClientResponseStream
    {
        #region Private fields
        private DeflateStream _stream;
        private Stream _baseStream;
        #endregion

        #region Constructor
        public HttpWebClientDeflateResponseStream(Stream baseStream)
        {
            _baseStream = baseStream;
            _stream = new DeflateStream(_baseStream, CompressionMode.Decompress);
        }
        #endregion

        #region implemented abstract members of Stream
        public override void Close()
        {
            Flush();
            _stream.Close();
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int ReadByte()
        {
            var buffer = new byte[1];
            var bytesRead = _stream.Read(buffer, 0, buffer.Length);
            return bytesRead == 1 ? buffer[0] : -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytes = _stream.Read(buffer, offset, count);
            return bytes;
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

        public override long Length { get { return _baseStream.Length; } }

        public override long Position
        {
            get
            {
                return _baseStream.Position;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IHttpWebClientResponseStream implementation
        public bool SocketForceClose
        {
            set
            {
                var socketStream = _baseStream as IHttpWebClientResponseStream;
                if (socketStream != null)
                {
                    socketStream.SocketForceClose = value;
                }
            }
        }

        public int Available
        {
            get
            {
                var socketStream = _baseStream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.Available : 0;
            }
        }

        public int SocketAvailable
        {
            get
            {
                var socketStream = _baseStream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.SocketAvailable : 0;
            }
        }

        public int BufferAvailable
        {
            get
            {
                var socketStream = _baseStream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.BufferAvailable : 0;
            }
        }

        public int SocketReceive(byte[] buffer, int offset, int count)
        {
            int bytes = 0;
            var socketStream = _baseStream as IHttpWebClientResponseStream;
            if (socketStream != null)
            {
                bytes = socketStream.SocketReceive(buffer, offset, count);
            }
            return bytes;
        }
        #endregion
    }
}
