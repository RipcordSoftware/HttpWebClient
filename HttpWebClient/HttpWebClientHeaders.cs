using System;
using System.Collections.Generic;

namespace RipcordSoftware.HttpWebClient
{
    public class HttpWebClientHeaders
    {
        #region Private fields
        private readonly Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Constructor
        public HttpWebClientHeaders()
        {
            Method = "GET";
            Uri = "/";
            Port = 80;
            Secure = false;
            ContentLength = null;
        }
        #endregion

        #region Public properties
        public string this [string key]
        { 
            get
            { 
                string value = null;
                headers.TryGetValue(key, out value);
                return value;
            }

            set
            {
                if (value != null)
                {
                    headers[key] = value;
                }
                else
                {
                    headers.Remove(key);
                }
            }
        }

        public string Method { get; set; }
        public string Uri { get; set; }
        public int Port { get; set; }
        public string Hostname { get; set; }
        public bool Secure { get; set; }
        public long? ContentLength { get; set; }
        #endregion

        #region Public methods
        public byte[] GetHeaderBytes()
        {
            return System.Text.Encoding.ASCII.GetBytes(GetHeaders());
        }

        public string GetHeaders()
        {
            var header = new System.Text.StringBuilder();

            header.AppendFormat("{0} {1} HTTP/1.1\r\n", Method, Uri);
            header.AppendFormat("Host: {0}:{1}\r\n", Hostname, Port);

            foreach (var pair in headers)
            {
                header.AppendFormat("{0}: {1}\r\n", pair.Key, pair.Value);
            }

            if (ContentLength.HasValue)
            {
                header.AppendFormat("Content-Length: {0}\r\n", ContentLength.Value);
            }

            header.Append("\r\n");

            return header.ToString();
        }
        #endregion
    }
}

