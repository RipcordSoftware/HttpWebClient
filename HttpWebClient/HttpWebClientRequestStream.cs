using System;
using System.IO;

namespace RipcordSoftware.HttpWebClient
{
    internal class HttpWebClientRequestStream : Stream
    {
        #region Private fields
        private readonly HttpWebClientHeaders headers;

        private RequestStream requestStream;
        private ChunkedRequestStream chunkedStream;
        #endregion

        #region Types
        private class RequestStream : Stream
        {
            #region Private fields
            private readonly HttpWebClientSocket socket;

            private readonly byte[] streamBuffer;
            private int streamBufferPosition = 0;
            private long position = 0;
            #endregion

            #region Constructor
            public RequestStream(HttpWebClientSocket socket)
            {
                this.socket = socket;
                streamBuffer = new byte[7 * 1024];
            }
            #endregion

            #region Protected properties
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Close();
                }
            }
            #endregion

            #region implemented abstract members of Stream
            public override void Flush()
            {
                socket.Flush();
            }
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
            public override long Seek(long offset, System.IO.SeekOrigin origin)
            {
                throw new NotImplementedException();
            }
            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                try
                {
                    if ((streamBufferPosition + count) >= streamBuffer.Length)
                    {
                        SendBuffer(socket, streamBuffer, 0, streamBufferPosition);
                        SendBuffer(socket, buffer, offset, count);
                        streamBufferPosition = 0;
                    }
                    else
                    {
                        Array.Copy(buffer, offset, streamBuffer, streamBufferPosition, count);
                        streamBufferPosition += count;
                    }

                    position += count;
                }
                catch (HttpWebClientRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new HttpWebClientRequestException("Error sending request", ex);
                }
            }
            public override bool CanRead
            {
                get
                {
                    return false;
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
                    return true;
                }
            }
            public override long Length
            {
                get
                {
                    return position;
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
            public override void Close()
            {
                if (streamBufferPosition > 0)
                {
                    SendBuffer(socket, streamBuffer, 0, streamBufferPosition);
                    position += streamBufferPosition;
                    streamBufferPosition = 0;
                }

                Flush();
            }
            #endregion

            #region Private methods
            private static void SendBuffer(HttpWebClientSocket socket, byte[] buffer, int offset, int count)
            {
                var sent = 0;
                while (sent < count)
                {
                    var bytes = socket.Send(buffer, offset + sent, count - sent);
                    if (bytes == 0)
                    {
                        throw new HttpWebClientRequestException("Failed to send buffer to remote, the socket returned 0 bytes sent");
                    }

                    sent += bytes;
                }
            }
            #endregion
        }

        private class ChunkedRequestStream : Stream
        {
            #region Constants
            private const int maxRequestChunkSize = 2048;
            private const string endOfLine = "\r\n";
            #endregion

            #region Private fields
            private static byte[] maxBlockSizeHeader = GetChunkHeader(maxRequestChunkSize);
            private static byte[] endResponseHeader = GetChunkHeader(0);
            private static byte[] endOfLineBytes = System.Text.Encoding.ASCII.GetBytes(endOfLine);

            private readonly byte[] streamBuffer;

            private RequestStream stream = null;
            private long position = 0;
            #endregion

            #region Constructor
            public ChunkedRequestStream(RequestStream stream)
            {
                this.stream = stream;

                streamBuffer = new byte[maxBlockSizeHeader.Length + maxRequestChunkSize + endOfLineBytes.Length];
            }
            #endregion

            #region Public methods
            public override void Write(byte[] buffer, int offset, int count)
            {
                var blocks = count / maxRequestChunkSize;
                var overflow = count % maxRequestChunkSize;

                if (blocks > 0)
                {
                    // copy the chunk header into the stream buffer
                    Array.Copy(maxBlockSizeHeader, streamBuffer, maxBlockSizeHeader.Length);

                    // copy the chunk trailer into the stream buffer
                    Array.Copy(endOfLineBytes, 0, streamBuffer, streamBuffer.Length - endOfLineBytes.Length, endOfLineBytes.Length);

                    for (int i = 0; i < blocks; i++)
                    {
                        // copy in the chunk data
                        Array.Copy(buffer, offset, streamBuffer, maxBlockSizeHeader.Length, maxRequestChunkSize);
                        offset += maxRequestChunkSize;

                        // write the buffer
                        stream.Write(streamBuffer, 0, streamBuffer.Length);

                        position += streamBuffer.Length;
                    }
                }

                if (overflow > 0)
                {
                    // get the chunk overflow header
                    var header = GetChunkHeader(overflow);

                    // copy the header into the stream buffer
                    Array.Copy(header, streamBuffer, header.Length);
                    int overflowLength = header.Length;

                    // copy the chunk body
                    Array.Copy(buffer, offset, streamBuffer, overflowLength, overflow);
                    overflowLength += overflow;

                    // copy the chunk trailer
                    Array.Copy(endOfLineBytes, 0, streamBuffer, overflowLength, endOfLineBytes.Length);
                    overflowLength += endOfLineBytes.Length;

                    // write the overflow data into the socket
                    stream.Write(streamBuffer, 0, overflowLength);

                    position += overflowLength;
                }
            }
            #endregion

            #region Protected methods
            protected override void Dispose(bool disposing)
            {
                if (disposing && stream != null)
                {
                    // the response finishes with a \r\n
                    stream.Write(endResponseHeader, 0, endResponseHeader.Length);

                    stream.Close();
                    stream = null;
                }
            }            
            #endregion

            #region Private methods
            private static byte[] GetChunkHeader(int size)
            {
                var format = "{0:X}" + endOfLine + (size == 0 ? endOfLine : string.Empty);
                var text = string.Format(format, size);
                return System.Text.Encoding.ASCII.GetBytes(text);
            }
            #endregion

            #region implemented abstract members of Stream
            public override void Flush()
            {
                if (stream != null)
                {
                    stream.Flush();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override bool CanRead
            {
                get
                {
                    return false;
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
                    return true;
                }
            }

            public override long Length
            {
                get
                {
                    return position;
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
        }
        #endregion

        #region Constructor
        public HttpWebClientRequestStream(HttpWebClientSocket socket, HttpWebClientHeaders headers)
        {
            this.headers = headers;

            requestStream = new RequestStream(socket);
        }
        #endregion

        #region implemented abstract members of Stream
        public override void Flush()
        {
            if (chunkedStream != null)
            {
                chunkedStream.Flush();
            }
            else if (requestStream != null)
            {
                requestStream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
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
            if (requestStream != null)
            {
                if (requestStream.Position == 0)
                {
                    if (!headers.ContentLength.HasValue)
                    {
                        headers["Transfer-Encoding"] = "chunked";
                        chunkedStream = new ChunkedRequestStream(requestStream);
                    }

                    SendHeaders();
                }

                if (chunkedStream != null)
                {
                    chunkedStream.Write(buffer, offset, count);
                }
                else
                {
                    requestStream.Write(buffer, offset, count);
                }
            }
        }

        public override bool CanRead
        {
            get
            {
                return false;
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
                return true;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                return chunkedStream != null ? chunkedStream.Position : 0;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region Private and protected methods
        protected override void Dispose(bool disposing)
        {
            if (disposing && requestStream != null)
            {
                try
                {
                    if (requestStream.Position == 0)
                    {
                        headers["Transfer-Encoding"] = null;

                        if (headers.Method == "PUT" || headers.Method == "POST")
                        {
                            headers.ContentLength = 0;
                        }

                        SendHeaders();
                    }
                }
                finally
                {
                    if (chunkedStream != null)
                    {
                        chunkedStream.Close();
                    }
                    else
                    {
                        requestStream.Close();
                    }

                    chunkedStream = null;
                    requestStream = null;
                }
            }
        }

        private void SendHeaders()
        {
            try
            {
                var bytes = headers.GetHeaderBytes();
                requestStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                var msg = string.Format("Unable to send headers to the remote host {0}:{1}", headers.Hostname, headers.Port);
                throw new HttpWebClientRequestException(msg, ex);
            }
        }
        #endregion
    }
}