using System;
using System.Collections.Generic;
using System.IO;

namespace RipcordSoftware.HttpWebClient
{
    public class HttpWebClientRequest
    {
        #region Constants
        private const int defaultSocketTimeout = 30 * 1000;
        #endregion

        #region Private fields
        private readonly HttpWebClientHeaders headers = new HttpWebClientHeaders();

        private HttpWebClientSocket socket = null;
        private HttpWebClientResponse response = null;

        private int socketTimeout = defaultSocketTimeout;
        #endregion

        #region Constructor
        private HttpWebClientRequest()
        {
            Method = "GET";
            UserAgent = "Mozilla/5.0 TachosDBWebClient/1.0";
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

            headers.Hostname = hostname;
            headers.Port = port;
            headers.Uri = uri;
            headers.Secure = secure;
        }

        public HttpWebClientRequest(string url) : this()
        {
            headers.Secure = url.StartsWith("https://");
            if (headers.Secure)
            {
                headers.Port = 443;
            }

            if (!headers.Secure && !url.StartsWith("http://"))
            {
                throw new HttpWebClientRequestException("Malformed URL - invalid request type");
            }

            var hostStartIndex = headers.Secure ? 8 : 7;
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
                    headers.Port = int.Parse(portText);
                    hostname = hostname.Substring(0, portIndex);
                }
                catch (Exception ex)
                {
                    throw new HttpWebClientRequestException("Malformed URL - bad port specification", ex);
                }
            }

            headers.Hostname = hostname;

            if (hostEndIndex >= 0)
            {
                headers.Uri = url.Substring(hostEndIndex);
            }
        }
        #endregion

        #region Public properties
        public string UserAgent { get { return headers["User-Agent"]; } set { headers["User-Agent"] = value; } }
        public string Accept { get { return headers["Accept"]; } set { headers["Accept"] = value; } }
        public string AcceptEncoding { get { return headers["Accept-Encoding"]; } set { headers["Accept-Encoding"] = value; } }
        public string ContentType { get { return headers["Content-Type"]; } set { headers["Content-Type"] = value; } }
        public string ContentEncoding { get { return headers["Content-Encoding"]; } set { headers["Content-Encoding"] = value; } }
        public string TransferEncoding { get { return headers["Transfer-Encoding"]; } set { headers["Transfer-Encoding"] = value; } }

        public HttpWebClientHeaders Headers { get { return headers; } }

        public string Method { get { return headers.Method; } set { headers.Method = value; } }
        public long ContentLength { get { return headers.ContentLength ?? 0; } set { headers.ContentLength = value; } }

        public int Timeout { get { return socketTimeout; } set { if (socket != null) { socket.Timeout = value; } socketTimeout = value; } }
        #endregion

        #region Public methods
        public Stream GetRequestStream()
        {
            if (socket != null)
            {
                throw new HttpWebClientRequestException("GetRequestStream() cannot be called more than once");
            }

            try
            {
                socket = HttpWebClientSocket.GetSocket(headers.Hostname, headers.Port, socketTimeout);
            }
            catch (Exception ex)
            {
                var msg = string.Format("Unable to connect to the remote host {0}:{1}", headers.Hostname, headers.Port);
                throw new HttpWebClientRequestException(msg, ex);
            }
                
            var stream = new HttpWebClientRequestStream(socket, headers);
            return stream;
        }
            
        public HttpWebClientResponse GetResponse(bool throwOnError = true)
        {
            if (response == null)
            {
                if (socket == null)
                {
                    using (GetRequestStream());
                }

                response = new HttpWebClientResponse(socket, throwOnError);
            }

            return response;
        }
        #endregion
    }
}