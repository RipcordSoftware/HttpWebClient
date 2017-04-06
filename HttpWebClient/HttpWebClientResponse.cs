//The MIT License(MIT)
//
//Copyright(c) 2015-2017 Ripcord Software Ltd
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
    public class HttpWebClientResponse : IDisposable
    {
        #region Constants
        private const int DefaultKeepAliveTimeout = 5000;
        #endregion

        #region Private fields
        private Stream _responseStream;

        private readonly HttpWebClientHeaders _headers = new HttpWebClientHeaders();

        private long? _contentLength;

        private readonly byte[] _tempBuffer = new byte[256];
        #endregion

        #region Constructor
        internal HttpWebClientResponse(IHttpWebClientSocket socket, bool throwOnError)
        {
            try
            {
                var memStream = GetHeaders(socket);

                if (string.Compare(Connection, "close", true) != 0)
                {
                    int? keepAliveTimeout = null;

                    if (string.Compare(Connection, "Keep-Alive", true) == 0)
                    {
                        keepAliveTimeout = KeepAliveTimeout;
                    }

                    socket.KeepAliveOnClose(keepAliveTimeout ?? DefaultKeepAliveTimeout);
                }

                var stream = new HttpWebClientResponseStream(socket, memStream, _contentLength);

                if (string.Compare(TransferEncoding, "chunked", true) == 0)
                {
                    _responseStream = new HttpWebClientChunkedResponseStream(stream);
                }
                else
                {
                    _responseStream = stream;
                }

                switch (ContentEncoding)
                {
                    case "deflate":
                        _responseStream = new HttpWebClientDeflateResponseStream(_responseStream);
                        break;

                    case "gzip":
                        _responseStream = new HttpWebClientGZipResponseStream(_responseStream);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new HttpWebClientResponseException("Unable to read response headers", ex);
            }

            // the standard .NET web client throws on 'error' cases
            if (throwOnError && StatusCode >= 400)
            {
                var errStream = new MemoryStream();

                // read the response stream into memory 
                _responseStream.CopyTo(errStream);
                errStream.Position = 0;

                // close the response stream
                _responseStream.Dispose();
                _responseStream = null;

                var msg = string.Format("{0} {1}", StatusCode, StatusDescription);
                throw new HttpWebClientResponseStatusException(msg, StatusCode, StatusDescription, Headers, errStream.ToArray());
            }
        }
        #endregion

        #region Public methods
        public Stream GetResponseStream()
        {
            return _responseStream;
        }
        #endregion

        #region Public properties
        public string Connection { get { return _headers["Connection"]; } }
        public string KeepAlive { get { return _headers["Keep-Alive"]; } }
        public string ContentType { get { return _headers["Content-Type"]; } }
        public string ContentEncoding { get { return _headers["Content-Encoding"]; } }
        public string TransferEncoding { get { return _headers["Transfer-Encoding"]; } }

        public int? KeepAliveTimeout
        {
            get
            {
                int? timeout = null;

                var ka = KeepAlive;
                if (!string.IsNullOrEmpty(ka))
                {
                    var timeoutStartIndex = ka.IndexOf("timeout=");
                    if (timeoutStartIndex >= 0)
                    {
                        timeoutStartIndex += 8;

                        var timeoutEndIndex = ka.IndexOf(" ", timeoutStartIndex);
                        if (timeoutEndIndex <= timeoutStartIndex)
                        {
                            timeoutEndIndex = ka.Length;
                        }

                        var timeoutText = ka.Substring(timeoutStartIndex, timeoutEndIndex - timeoutStartIndex);

                        int timeoutValue;
                        if (int.TryParse(timeoutText, out timeoutValue))
                        {
                            timeout = timeoutValue * 1000;
                        }
                    }
                }

                return timeout;
            }
        }

        public HttpWebClientHeaders Headers { get { return _headers; } }

        public long ContentLength { get { return _contentLength.HasValue ? _contentLength.Value : 0; } }
        public int StatusCode { get; protected set; }
        public string StatusDescription { get; protected set; }
        #endregion

        #region Private methods
        private MemoryStream GetHeaders(IHttpWebClientSocket socket)
        {
            var buffer = new byte[7 * 1024];
            int bufferOffset = 0;
            int bufferLength = 0;
            int headersLength = 0;

            while (headersLength == 0 && bufferLength < buffer.Length)
            {
                int bytes = 0;

                try
                {
                    bytes = socket.Receive(buffer, bufferOffset, buffer.Length - bufferOffset);
                }
                catch (Exception ex)
                {
                    throw new HttpWebClientResponseException("Error reading response from remote", ex);
                }

                if (bytes <= 0)
                {
                    throw new HttpWebClientResponseException("Failed to read response header from remote");
                }

                // we need to include the last character of the previous pass when searching for the header end
                if (bufferOffset > 0)
                {
                    bufferOffset--;
                }

                bufferLength += bytes;
                for (int i = bufferOffset; i < bufferLength - 3; i++, bufferOffset++)
                {
                    if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
                    {
                        headersLength = i + 4;
                        break;
                    }
                }                
            }

            if (headersLength == 0 && bufferLength == buffer.Length)
            {
                throw new HttpWebClientResponseException("Failed to read response header from remote, the header is too large");
            }

            ParseHeaders(buffer, headersLength);

            var stream = new MemoryStream(buffer, headersLength, bufferLength - headersLength, false);
            return stream;
        }

        private void ParseHeaders(byte[] buffer, int bufferLength)
        {
            var headerText = System.Text.Encoding.ASCII.GetString(buffer, 0, bufferLength);

            if (!headerText.StartsWith("HTTP/1.", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpWebClientResponseException("The response headers could not be parsed; missing HTTP directive");
            }

            var headers = headerText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (headers.Length < 1)
            {
                throw new HttpWebClientResponseException("The response headers could not be parsed");
            }

            // if the server responded with a 100-continue we need to eat it here
            int headerStartIndex = 0;
            if (string.CompareOrdinal(headers[0], "HTTP/1.1 100 Continue") == 0)
            {
                headerStartIndex = 1;
            }

            int statusStartIndex = headers[headerStartIndex].IndexOf(' ');
            if (statusStartIndex <= 0)
            {
                throw new HttpWebClientResponseException("The response headers could not be parsed; malformed status line");
            }

            statusStartIndex++;

            int descStartIndex = headers[headerStartIndex].IndexOf(' ', statusStartIndex);
            if (descStartIndex <= statusStartIndex)
            {
                descStartIndex = headers[headerStartIndex].Length;
            }

            var statusCodeText = headers[headerStartIndex].Substring(statusStartIndex, descStartIndex - statusStartIndex);
            var description = headers[headerStartIndex].Substring(descStartIndex);

            int statusCode = -1;
            if (!int.TryParse(statusCodeText, out statusCode))
            {
                throw new HttpWebClientResponseException("The response headers could not be parsed; invalid status code");
            }

            StatusCode = statusCode;
            StatusDescription = description;

            for (int i = headerStartIndex; i < headers.Length; i++)
            {
                int seperatorIndex = headers[i].IndexOf(':');
                if (seperatorIndex > 0)
                {
                    string value = null;
                    for (int j = seperatorIndex + 1; value == null && j < headers[i].Length; j++)
                    {
                        if (headers[i][j] != ' ')
                        {
                            value = headers[i].Substring(j).TrimEnd();
                        }
                    }

                    if (value != null)
                    {
                        var key = headers[i].Substring(0, seperatorIndex).Trim();
                        this._headers[key] = value;
                    }
                }
            }

            var contentLengthText = this._headers["Content-Length"];
            if (contentLengthText != null)
            {
                long contentLength = -1;
                if (!long.TryParse(contentLengthText, out contentLength))
                {
                    throw new HttpWebClientResponseException("The response headers contain an invalid content length value");
                }

                this._contentLength = contentLength;
            }
        }

        private void SkipStreamBytes(Stream stream, int count)
        {
            int read = 0;
            while (read < count)
            {
                read += stream.Read(_tempBuffer, 0, Math.Min(_tempBuffer.Length, count - read));
            }
        }

        private void SkipSocketBytes(IHttpWebClientResponseStream stream, int count)
        {
            int read = 0;
            while (read < count)
            {
                read += stream.SocketReceive(_tempBuffer, 0, Math.Min(_tempBuffer.Length, count - read));
            }
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            if (_responseStream != null)
            {
                // before we finish with the response we should try to drain the stream of any remaining data
                // this will allow us to use the socket for another request/response without returning it to the OS
                var clientStream = _responseStream as IHttpWebClientResponseStream;
                if (clientStream != null)
                {
                    if (string.Compare(Connection, "close", true) == 0)
                    {
                        clientStream.SocketForceClose = true;
                    }
                    else if (_contentLength.HasValue)
                    {
                        var remainingBytes = _contentLength.Value - _responseStream.Position;
                        if (remainingBytes > 0)
                        {
                            if (remainingBytes == clientStream.BufferAvailable || remainingBytes == clientStream.Available)
                            {
                                SkipStreamBytes(_responseStream, (int)remainingBytes);
                            }
                            else if (remainingBytes <= 65536)
                            {
                                int retry = 50;
                                while (retry-- > 0)
                                {
                                    System.Threading.Thread.Sleep(1);
                                    if (remainingBytes == clientStream.Available)
                                    {
                                        SkipStreamBytes(_responseStream, (int)remainingBytes);
                                        break;
                                    }
                                }
                            }

                            if (_responseStream.Position < _contentLength.Value)
                            {
                                clientStream.SocketForceClose = true;
                            }
                        }
                    }
                    else if (TransferEncoding == "chunked" && clientStream.SocketAvailable >= 5)
                    {
                        // ignore all but the last 5 bytes
                        var skipBytes = clientStream.SocketAvailable - 5;
                        SkipSocketBytes(clientStream, skipBytes);

                        // the last 5 bytes may contain the chunked response end, if it doesn't then we want to force the socket closed
                        var responseEndBuffer = new byte[5];
                        clientStream.SocketReceive(responseEndBuffer, 0, responseEndBuffer.Length);
                        if (responseEndBuffer[0] != '0' || responseEndBuffer[1] != '\r' || responseEndBuffer[2] != '\n' || responseEndBuffer[3] != '\r' || responseEndBuffer[4] != '\n')
                        {
                            clientStream.SocketForceClose = true;
                        }
                    }
                }

                _responseStream.Dispose();
                _responseStream = null;
            }
        }
        #endregion
    }
}
