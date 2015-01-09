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
        #region Constants
        private int maxResponseChunkSize = 32768;
        #endregion

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
        #endregion

        #region Private fields
        private static readonly byte[] lastChunkSig = new byte[] { 0x30, 0x0d, 0x0a, 0x0d, 0x0a };

        private HttpWebClientResponseStream stream;

        private MemoryStream memStream = null;

        private long length = 0;
        private long position = 0;
        #endregion

        #region Constructor
        public HttpWebClientChunkedResponseStream(HttpWebClientResponseStream stream)
        {
            this.stream = stream;
        }
        #endregion

        #region implemented abstract members of Stream
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;

            if (memStream == null)
            {
                memStream = GetChunk();
            }

            if (memStream != null)
            {
                read = memStream.Read(buffer, offset, count);

                length += read;
                position += read;

                if (memStream.Position == memStream.Length)
                {
                    memStream = null;
                }
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
                return length;
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
        protected override void Dispose(bool disposing)
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
        #endregion

        #region IHttpWebClientResponseStream implementation
        public bool SocketForceClose
        {
            set
            {
                var socketStream = stream as IHttpWebClientResponseStream;
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
                var socketStream = stream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.Available : 0;
            }
        }

        public int SocketAvailable
        {
            get
            {
                var socketStream = stream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.SocketAvailable : 0;
            }
        }

        public int BufferAvailable
        {
            get
            {
                var socketStream = stream as IHttpWebClientResponseStream;
                return socketStream != null ? socketStream.BufferAvailable : 0;
            }
        }

        public int SocketReceive(byte[] buffer, int offset, int count)
        {
            int bytes = 0;
            var socketStream = stream as IHttpWebClientResponseStream;
            if (socketStream != null)
            {
                bytes = socketStream.SocketReceive(buffer, offset, count);
            }
            return bytes;
        }
        #endregion

        #region Private methods
        private MemoryStream GetChunk()
        {
            MemoryStream chunkStream = null;

            var tempBuffer = new byte[16];
            var tempBufferDataLength = 0;

            do
            {
                tempBufferDataLength = stream.Read(tempBuffer, 0, tempBuffer.Length, true);
            } while (tempBufferDataLength < tempBuffer.Length && !IsLastChunk(tempBuffer, tempBufferDataLength));

            if (tempBufferDataLength > 0)
            {
                var chunkHeader = GetChunkHeader(tempBuffer, tempBufferDataLength, maxResponseChunkSize);

                // eat the header since we know the size now
                stream.Read(tempBuffer, 0, chunkHeader.HeaderSize);

                if (chunkHeader.BlockSize > 0)
                {
                    chunkStream = new MemoryStream(chunkHeader.BlockSize);
                    var chunkBuffer = chunkStream.GetBuffer();

                    int bytesRead = 0;
                    do
                    {
                        bytesRead += stream.Read(chunkBuffer, bytesRead, chunkHeader.BlockSize - bytesRead);
                    } while (bytesRead < chunkHeader.BlockSize);                                                        

                    chunkStream.SetLength(bytesRead);
                }

                // we are at the end of the block, eat the trailing \r\n
                stream.Read(tempBuffer, 0, 2);
            }

            return chunkStream;
        }

        private static ChunkHeader GetChunkHeader(byte[] buffer, int dataLength, int maxRequestChunkSize)
        {
            ChunkHeader header = null;
            int i = 0;

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

            if (length > maxRequestChunkSize)
            {
                var msg = string.Format("The response chunk size ({0}) is too large", length);
                throw new HttpWebClientResponseException(msg);
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

        private bool IsLastChunk(byte[] buffer, int dataLength)
        {
            dataLength = Math.Min(buffer.Length, dataLength);

            int sigIndex = 0;
            for (int i = 0; i < dataLength; i++)
            {
                if (buffer[i] == lastChunkSig[sigIndex])
                {
                    sigIndex++;
                }
                else
                {
                    sigIndex = 0;
                }
            }

            return sigIndex == lastChunkSig.Length;
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