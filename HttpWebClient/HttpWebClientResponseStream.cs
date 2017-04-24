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
    internal interface IHttpWebClientResponseStream
    {
        int Available { get; }
        int BufferAvailable { get; }
        int SocketAvailable { get; }

        bool SocketForceClose { set; }
        int SocketReceive(byte[] buffer, int offset, int count);
    }

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

    internal sealed class HttpWebClientChunkedResponseStream : Stream, IHttpWebClientResponseStream
    {
        #region Types
        private class ChunkHeader
        {
            public ChunkHeader(int blockSize, int headerSize)
            {
                BlockSize = blockSize;
                HeaderSize = headerSize;
            }

            public int BlockSize { get; protected set; }
            public int HeaderSize { get; protected set; }
        }

        private class ChunkDescriptor
        {
            public ChunkDescriptor(int blockSize)
            {
                BlockSize = blockSize;
            }

            public int IncrementOffset(int delta)
            {
                Offset += delta;
                return Offset;
            }

            public int BlockSize { get; protected set; }
            public int Offset { get; protected set; }
            public int Remaining { get { return BlockSize - Offset; } }
            public bool IsFinished { get { return Remaining == 0; } }
        }
        #endregion

        #region Private fields
        private HttpWebClientResponseStream _stream;

        private long _length = 0;
        private long _position = 0;

        private ChunkDescriptor _chunk = null;
        #endregion

        #region Constructor
        public HttpWebClientChunkedResponseStream(HttpWebClientResponseStream stream)
        {
            _stream = stream;
        }
        #endregion

        #region implemented abstract members of Stream
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;

            if (_chunk == null)
            {
                _chunk = GetChunk();
            }

            if (_chunk != null && !_chunk.IsFinished)
            {
                count = Math.Min(count, _chunk.Remaining);

                read = _stream.Read(buffer, offset, count);
                if (read > 0)
                {
                    _chunk.IncrementOffset(read);
                    _length += read;
                    _position += read;
                }
            }

            if (_chunk != null && _chunk.IsFinished)
            {
                var temp = new byte[2];
                _stream.Read(temp, 0, temp.Length);

                _chunk = null;
            }

            return read;
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

        public override bool CanRead {  get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        public override long Length { get { return _length; } }

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
        public override void Close()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
        #endregion

        #region IHttpWebClientResponseStream implementation
        public bool SocketForceClose
        {
            set
            {
                var socketStream = _stream as IHttpWebClientResponseStream;
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
                var socketStream = _stream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.Available : 0;
            }
        }

        public int SocketAvailable
        {
            get
            {
                var socketStream = _stream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.SocketAvailable : 0;
            }
        }

        public int BufferAvailable
        {
            get
            {
                var socketStream = _stream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.BufferAvailable : 0;
            }
        }

        public int SocketReceive(byte[] buffer, int offset, int count)
        {
            int bytes = 0;
            var socketStream = _stream as IHttpWebClientResponseStream;
            if (socketStream != null)
            {
                bytes = socketStream.SocketReceive(buffer, offset, count);
            }
            return bytes;
        }
        #endregion

        #region Private methods
        private ChunkDescriptor GetChunk()
        {
            ChunkDescriptor chunk = null;
            var buffer = new byte[16];
            var totalRead = 0;
            var read = 0;

            do
            {
                read = _stream.Read(buffer, 0, buffer.Length, true);
                if (read > 0)
                {
                    totalRead += read;
                }

            } while (read > 0 && totalRead < buffer.Length);

            if (totalRead > 0)
            {
                var chunkHeader = GetChunkHeader(buffer, totalRead);

                // eat the header since we know the size now
                _stream.Read(buffer, 0, chunkHeader.HeaderSize);

                chunk = new ChunkDescriptor(chunkHeader.BlockSize);
            }

            return chunk;
        }

        private static ChunkHeader GetChunkHeader(byte[] buffer, int dataLength)
        {
            ChunkHeader header = null;
            var i = 0;

            dataLength = Math.Min(buffer.Length, dataLength);

            // skip the trailing end of the previous block
            if ((dataLength > 0 && buffer[0] == '\n'))
            {
                i++;
            }
            else if (dataLength > 0 && buffer[0] == '\r' && buffer[1] == '\n')
            {
                i += 2;
            }

            int length = 0;
            for (; i < dataLength && buffer[i] != '\r' && buffer[i] != '\n'; i++)
            {
                length *= 16;

                var value = buffer[i];
                if (value >= '0' && value <= '9')
                {
                    value -= 48;
                }
                else if (value >= 'a' && value <= 'f')
                {
                    value -= (97 - 10);
                }
                else if (value >= 'A' && value <= 'F')
                {
                    value -= (65 - 10);
                }
                else
                {
                    throw new HttpWebClientResponseException("The response chunk data is malformed");
                }

                length += value;
            }

            if (buffer[i] == '\n' || (buffer[i++] == '\r' && i < dataLength && buffer[i] == '\n'))
            {
                header = new ChunkHeader(length, i + 1);
            }
            else
            {               
                throw new HttpWebClientResponseException("The response chunk data is malformed");
            }

            return header;
        }
        #endregion
    }

    internal sealed class HttpWebClientGZipResponseStream : Stream, IHttpWebClientResponseStream
    {
        #region Private fields
        private GZipStream _stream;
        private Stream _baseStream;
        #endregion

        #region Constructor
        public HttpWebClientGZipResponseStream(Stream baseStream)
        {
            _baseStream = baseStream;
            _stream = new GZipStream(_baseStream, CompressionMode.Decompress);
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
