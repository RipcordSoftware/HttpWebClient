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

    internal class HttpWebClientResponseStream : Stream, IHttpWebClientResponseStream
    {
        #region Private fields
        private HttpWebClientSocket socket;

        private long position = 0;
        private long? length;

        private MemoryStream memStream;
        #endregion

        #region Constructor
        public HttpWebClientResponseStream(HttpWebClientSocket socket, MemoryStream memStream, long? length)
        {
            this.socket = socket;
            this.memStream = memStream;
            this.length = length;
        }
        #endregion

        #region implemented abstract members of Stream
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
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }
        public override long Length
        {
            get
            {
                return length.HasValue ? length.Value : position;
            }
        }
        public override long Position
        {
            get
            {
                return position;
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
            long wantLength = length.HasValue ? length.Value : long.MaxValue;

            if (memStream != null && position < wantLength)
            {
                var startPos = memStream.Position;
                memStream.Read(buffer, offset, count);
                size = (int)(memStream.Position - startPos);

                if (peek)
                {
                    memStream.Position = startPos;
                }
                else
                {
                    if (memStream.Position == memStream.Length)
                    {
                        memStream = null;
                    }

                    position += size;
                }
                
                offset += size;
                count -= size;
            }

            if ((size == 0 || socket.Available > 0) && count > 0 && position < wantLength)
            {
                var received = socket.Receive(buffer, offset, count, peek);
                size += received;

                if (!peek)
                {
                    position += received;
                }
            }

            return size;
        }
        #endregion

        #region Protected methods
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (socket != null)
                {
                    socket.Close();
                    socket = null;
                }
            }

            base.Dispose(disposing);
        }
        #endregion

        #region IHttpWebClientResponseStream implementation
        public int Available
        {
            get
            {
                return BufferAvailable + SocketAvailable;
            }
        }

        public int SocketAvailable
        {
            get
            {
                return socket != null ? socket.Available : 0;
            }
        }

        public int BufferAvailable
        {
            get
            {
                return memStream != null ? (int)(memStream.Length - memStream.Position) : 0;
            }
        }

        public bool SocketForceClose
        {
            set
            {
                if (socket != null)
                {
                    socket.ForceClose = value;
                }
            }
        }
            
        public int SocketReceive(byte[] buffer, int offset, int count)
        {
            return socket != null ? socket.Receive(buffer, offset, count) : 0;
        }
        #endregion
    }

    internal class HttpWebClientChunkedResponseStream : Stream, IHttpWebClientResponseStream
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

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return _length;
            }
        }

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
        protected override void Dispose(bool disposing)
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

    internal class HttpWebClientGZipResponseStream : Stream, IHttpWebClientResponseStream
    {
        #region Private fields
        private GZipStream stream;
        private Stream baseStream;
        #endregion

        #region Constructor
        public HttpWebClientGZipResponseStream(Stream baseStream)
        {
            this.baseStream = baseStream;
            stream = new GZipStream(this.baseStream, CompressionMode.Decompress);
        }
        #endregion

        #region implemented abstract members of Stream
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytes = stream.Read(buffer, offset, count);
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

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return baseStream.Position;
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
                var socketStream = baseStream as IHttpWebClientResponseStream;
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
                var socketStream = baseStream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.Available : 0;
            }
        }

        public int SocketAvailable
        {
            get
            {
                var socketStream = baseStream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.SocketAvailable : 0;
            }
        }

        public int BufferAvailable
        {
            get
            {
                var socketStream = baseStream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.BufferAvailable : 0;
            }
        }

        public int SocketReceive(byte[] buffer, int offset, int count)
        {
            int bytes = 0;
            var socketStream = baseStream as IHttpWebClientResponseStream;
            if (socketStream != null)
            {
                bytes = socketStream.SocketReceive(buffer, offset, count);
            }
            return bytes;
        }
        #endregion

        #region Protected methods
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                stream.Dispose();
            }
        }
        #endregion
    }

    internal class HttpWebClientDeflateResponseStream : Stream, IHttpWebClientResponseStream
    {
        #region Private fields
        private DeflateStream stream;
        private Stream baseStream;
        #endregion

        #region Constructor
        public HttpWebClientDeflateResponseStream(Stream baseStream)
        {
            this.baseStream = baseStream;
            stream = new DeflateStream(this.baseStream, CompressionMode.Decompress);
        }
        #endregion

        #region implemented abstract members of Stream
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytes = stream.Read(buffer, offset, count);
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

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return baseStream.Position;
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
                var socketStream = baseStream as IHttpWebClientResponseStream;
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
                var socketStream = baseStream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.Available : 0;
            }
        }

        public int SocketAvailable
        {
            get
            {
                var socketStream = baseStream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.SocketAvailable : 0;
            }
        }

        public int BufferAvailable
        {
            get
            {
                var socketStream = baseStream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.BufferAvailable : 0;
            }
        }

        public int SocketReceive(byte[] buffer, int offset, int count)
        {
            int bytes = 0;
            var socketStream = baseStream as IHttpWebClientResponseStream;
            if (socketStream != null)
            {
                bytes = socketStream.SocketReceive(buffer, offset, count);
            }
            return bytes;
        }
        #endregion

        #region Protected methods
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                stream.Dispose();
            }
        }
        #endregion
    }
}
