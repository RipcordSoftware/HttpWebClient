using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace RipcordSoftware.HttpWebClient
{
    public class HttpWebClientResponse : IDisposable
    {
        #region Constants
        private const int defaultKeepAliveTimeout = 5000;
        #endregion

        #region Private fields
        private Stream responseStream;

        private readonly HttpWebClientHeaders headers = new HttpWebClientHeaders();

        private long? contentLength;

        private readonly byte[] tempBuffer = new byte[256];
        #endregion

        #region Constructor
        internal HttpWebClientResponse(HttpWebClientSocket socket, bool throwOnError)
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

                    socket.KeepAliveOnClose(keepAliveTimeout ?? defaultKeepAliveTimeout);
                }

                var stream = new HttpWebClientResponseStream(socket, memStream, contentLength);

                if (string.Compare(TransferEncoding, "chunked", true) == 0)
                {
                    responseStream = new HttpWebClientChunkedResponseStream(stream);
                }
                else
                {
                    responseStream = stream;
                }

                switch (ContentEncoding)
                {
                    case "deflate":
                        responseStream = new HttpWebClientDeflateResponseStream(responseStream);
                        break;

                    case "gzip":
                        responseStream = new HttpWebClientGZipResponseStream(responseStream);
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
                responseStream.CopyTo(errStream);
                errStream.Position = 0;

                // close the response stream
                responseStream.Dispose();
                responseStream = null;

                var msg = string.Format("{0} {1}", StatusCode, StatusDescription);
                throw new HttpWebClientResponseStatusException(msg, StatusCode, StatusDescription, Headers, errStream.ToArray());
            }
        }
        #endregion

        #region Public methods
        public Stream GetResponseStream()
        {
            return responseStream;
        }
        #endregion

        #region Public properties
        public string Connection { get { return headers["Connection"]; } }
        public string KeepAlive { get { return headers["Keep-Alive"]; } }
        public string ContentType { get { return headers["Content-Type"]; } }
        public string ContentEncoding { get { return headers["Content-Encoding"]; } }
        public string TransferEncoding { get { return headers["Transfer-Encoding"]; } }

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

        public HttpWebClientHeaders Headers { get { return headers; } }

        public long ContentLength { get { return contentLength.HasValue ? contentLength.Value : 0; } }
        public int StatusCode { get; protected set; }
        public string StatusDescription { get; protected set; }
        #endregion

        #region Private methods
        private MemoryStream GetHeaders(HttpWebClientSocket socket)
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
                        this.headers[key] = value;
                    }
                }
            }

            var contentLengthText = this.headers["Content-Length"];
            if (contentLengthText != null)
            {
                long contentLength = -1;
                if (!long.TryParse(contentLengthText, out contentLength))
                {
                    throw new HttpWebClientResponseException("The response headers contain an invalid content length value");
                }

                this.contentLength = contentLength;
            }
        }

        private void SkipStreamBytes(Stream stream, int count)
        {
            int read = 0;
            while (read < count)
            {
                read += stream.Read(tempBuffer, 0, Math.Min(tempBuffer.Length, count - read));
            }
        }

        private void SkipSocketBytes(IHttpWebClientResponseStream stream, int count)
        {
            int read = 0;
            while (read < count)
            {
                read += stream.SocketReceive(tempBuffer, 0, Math.Min(tempBuffer.Length, count - read));
            }
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            if (responseStream != null)
            {
                // before we finish with the response we should try to drain the stream of any remaining data
                // this will allow us to use the socket for another request/response without returning it to the OS
                var clientStream = responseStream as IHttpWebClientResponseStream;
                if (clientStream != null)
                {
                    if (string.Compare(Connection, "close", true) == 0)
                    {
                        clientStream.SocketForceClose = true;
                    }
                    else if (contentLength.HasValue)
                    {
                        var remainingBytes = contentLength.Value - responseStream.Position;
                        if (remainingBytes > 0)
                        {
                            if (remainingBytes == clientStream.BufferAvailable || remainingBytes == clientStream.Available)
                            {
                                SkipStreamBytes(responseStream, (int)remainingBytes);
                            }
                            else if (remainingBytes <= 65536)
                            {
                                int retry = 50;
                                while (retry-- > 0)
                                {
                                    System.Threading.Thread.Sleep(1);
                                    if (remainingBytes == clientStream.Available)
                                    {
                                        SkipStreamBytes(responseStream, (int)remainingBytes);
                                        break;
                                    }
                                }
                            }

                            if (responseStream.Position < contentLength.Value)
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

                responseStream.Dispose();
                responseStream = null;
            }
        }
        #endregion
    }
}
