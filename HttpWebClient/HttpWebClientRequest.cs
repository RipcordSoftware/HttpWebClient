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
    public class HttpWebClientRequest
    {
        #region Constants
        private const int DefaultSocketTimeout = 30 * 1000;
        #endregion

        #region Private fields
        private readonly HttpWebClientHeaders _headers = new HttpWebClientHeaders();

        private HttpWebClientSocket _socket = null;
        private HttpWebClientResponse _response = null;

        private int _socketTimeout = DefaultSocketTimeout;
        #endregion

        #region Constructor
        private HttpWebClientRequest()
        {
            Method = "GET";
            UserAgent = "Mozilla/5.0 RSHttpWebClient/1.0";
            Accept = "*/*";
            AcceptEncoding = "gzip, deflate";
        }

        public HttpWebClientRequest(string hostname, int port, string uri, bool secure = false) : this()
        {
            uri = uri ?? "/";
            if (uri.Length > 0 && uri[0] != '/')
            {
                uri = "/" + uri;
            }

            _headers.Hostname = hostname;
            _headers.Port = port;
            _headers.Uri = uri;
            _headers.Secure = secure;
        }

        public HttpWebClientRequest(string url) : this()
        {
            _headers.Secure = url.StartsWith("https://");
            if (_headers.Secure)
            {
                _headers.Port = 443;
            }

            if (!_headers.Secure && !url.StartsWith("http://"))
            {
                throw new HttpWebClientRequestException("Malformed URL - invalid request type");
            }

            var hostStartIndex = _headers.Secure ? 8 : 7;
            var hostEndIndex = url.IndexOf('/', hostStartIndex);
            if (hostEndIndex == hostStartIndex)
            {
                throw new HttpWebClientRequestException("Malformed URL - missing hostname");
            }
                
            var hostname = hostEndIndex >= 0 ? url.Substring(hostStartIndex, hostEndIndex - hostStartIndex) : url.Substring(hostStartIndex);

            var portIndex = hostname.IndexOf(':');
            if (portIndex > 0)
            {
                try
                {
                    var portText = hostname.Substring(portIndex + 1);
                    _headers.Port = int.Parse(portText);
                    hostname = hostname.Substring(0, portIndex);
                }
                catch (Exception ex)
                {
                    throw new HttpWebClientRequestException("Malformed URL - bad port specification", ex);
                }
            }

            _headers.Hostname = hostname;

            if (hostEndIndex >= 0)
            {
                _headers.Uri = url.Substring(hostEndIndex);
            }
        }
        #endregion

        #region Public properties
        public string UserAgent { get { return _headers["User-Agent"]; } set { _headers["User-Agent"] = value; } }
        public string Accept { get { return _headers["Accept"]; } set { _headers["Accept"] = value; } }
        public string AcceptEncoding { get { return _headers["Accept-Encoding"]; } set { _headers["Accept-Encoding"] = value; } }
        public string ContentType { get { return _headers["Content-Type"]; } set { _headers["Content-Type"] = value; } }
        public string ContentEncoding { get { return _headers["Content-Encoding"]; } set { _headers["Content-Encoding"] = value; } }
        public string TransferEncoding { get { return _headers["Transfer-Encoding"]; } set { _headers["Transfer-Encoding"] = value; } }

        public HttpWebClientHeaders Headers { get { return _headers; } }

        public string Method { get { return _headers.Method; } set { _headers.Method = value; } }
        public long ContentLength { get { return _headers.ContentLength ?? 0; } set { _headers.ContentLength = value; } }

        public int Timeout { get { return _socketTimeout; } set { if (_socket != null) { _socket.Timeout = value; } _socketTimeout = value; } }
        #endregion

        #region Public methods
        public Stream GetRequestStream()
        {
            if (_socket != null)
            {
                throw new HttpWebClientRequestException("GetRequestStream() cannot be called more than once");
            }

            try
            {
                _socket = HttpWebClientSocket.GetSocket(_headers.Hostname, _headers.Port, _socketTimeout);
            }
            catch (Exception ex)
            {
                var msg = string.Format("Unable to connect to the remote host {0}:{1}", _headers.Hostname, _headers.Port);
                throw new HttpWebClientRequestException(msg, ex);
            }
                
            var stream = new HttpWebClientRequestStream(_socket, _headers);
            return stream;
        }
            
        public HttpWebClientResponse GetResponse(bool throwOnError = true)
        {
            if (_response == null)
            {
                if (_socket == null)
                {
                    using (GetRequestStream()) { }
                }

                _response = new HttpWebClientResponse(_socket, throwOnError);
            }

            return _response;
        }
        #endregion
    }
}