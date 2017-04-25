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

namespace RipcordSoftware.HttpWebClient
{
    internal sealed class HttpWebClientRequestStream : Stream
    {
        #region Private fields
        private readonly HttpWebClientHeaders _headers;

        private readonly Stream _requestStream;
        private Stream _chunkedStream;
        #endregion

        #region Types
        internal sealed class RequestStream : Stream
        {
            #region Private fields
            private readonly IHttpWebClientSocket _socket;

            private readonly byte[] _streamBuffer;
            private int _streamBufferPosition = 0;
            private long _position = 0;
            #endregion

            #region Constructor
            public RequestStream(IHttpWebClientSocket socket)
            {
                _socket = socket;
                _streamBuffer = new byte[7 * 1024];
            }
            #endregion

            #region implemented abstract members of Stream
            public override void Flush()
            {
                _socket.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

            public override long Seek(long offset, System.IO.SeekOrigin origin) { throw new NotImplementedException(); }

            public override void SetLength(long value) { throw new NotImplementedException(); }

            public override void Write(byte[] buffer, int offset, int count)
            {
                try
                {
                    if ((_streamBufferPosition + count) >= _streamBuffer.Length)
                    {
                        SendBuffer(_socket, _streamBuffer, 0, _streamBufferPosition);
                        SendBuffer(_socket, buffer, offset, count);
                        _streamBufferPosition = 0;
                    }
                    else
                    {
                        Array.Copy(buffer, offset, _streamBuffer, _streamBufferPosition, count);
                        _streamBufferPosition += count;
                    }

                    _position += count;
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
            public override bool CanRead { get { return false; } }

            public override bool CanSeek { get { return false; } }

            public override bool CanWrite { get { return true; } }

            public override bool CanTimeout { get { return _socket.Timeout > 0; } }

            public override long Length { get { return _position; } }

            public override long Position { get { return _position; } set { throw new NotImplementedException(); } }
            #endregion

            #region Public methods
            public override void Close()
            {
                if (_streamBufferPosition > 0)
                {
                    SendBuffer(_socket, _streamBuffer, 0, _streamBufferPosition);
                    _position += _streamBufferPosition;
                    _streamBufferPosition = 0;
                }

                Flush();
            }
            #endregion

            #region Private methods
            private static void SendBuffer(IHttpWebClientSocket socket, byte[] buffer, int offset, int count)
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
        #endregion

        #region Constructor
        public HttpWebClientRequestStream(IHttpWebClientSocket socket, HttpWebClientHeaders headers) : this(new RequestStream(socket), headers)
        {
        }

        internal HttpWebClientRequestStream(Stream requestStream, HttpWebClientHeaders headers)
        {
            _requestStream = requestStream;
            _headers = headers;
        }
        #endregion

        #region Implemented abstract members of Stream
        public override void Flush()
        {
            if (_chunkedStream != null)
            {
                _chunkedStream.Flush();
            }
            else if (_requestStream != null)
            {
                _requestStream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }

        public override void SetLength(long value) { throw new NotImplementedException(); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_requestStream != null)
            {
                if (_requestStream.Position == 0)
                {
                    if (!_headers.ContentLength.HasValue)
                    {
                        _headers["Transfer-Encoding"] = "chunked";
                        _chunkedStream = new HttpWebClientChunkedRequestStream(_requestStream);
                    }

                    SendHeaders();
                }

                if (_chunkedStream != null)
                {
                    _chunkedStream.Write(buffer, offset, count);
                }
                else
                {
                    _requestStream.Write(buffer, offset, count);
                }
            }
        }

        public override bool CanTimeout { get { return _requestStream.CanTimeout; } }

        public override bool CanRead { get { return false; } }

        public override bool CanSeek { get { return false;  } }

        public override bool CanWrite { get { return true; } }

        public override long Length { get { return _requestStream.Length; } }

        public override long Position
        {
            get
            {
                return _requestStream.Position;
            }
            set
            {
                throw new NotImplementedException();
            }
        }        
        
        public override void Close()
        {
            try
            {
                if (_requestStream.Position == 0)
                {
                    _headers["Transfer-Encoding"] = null;

                    if (_headers.Method == "PUT" || _headers.Method == "POST")
                    {
                        _headers.ContentLength = 0;
                    }

                    SendHeaders();
                }
            }
            finally
            {
                try
                {
                    if (_chunkedStream != null)
                    {
                        _chunkedStream.Close();
                    }

                    _requestStream.Close();
                }
                catch (HttpWebClientException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var msg = string.Format("Failed to close socket to remote host {0}:{1}", _headers.Hostname, _headers.Port);
                    throw new HttpWebClientRequestException(msg, ex);
                }
            }
        }
        #endregion

        #region Private and protected methods
        private void SendHeaders()
        {
            try
            {
                var bytes = _headers.GetHeaderBytes();
                _requestStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                var msg = string.Format("Unable to send headers to the remote host {0}:{1}", _headers.Hostname, _headers.Port);
                throw new HttpWebClientRequestException(msg, ex);
            }
        }
        #endregion
    }
}
