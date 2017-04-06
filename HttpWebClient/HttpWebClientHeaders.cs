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
using System.Collections.Generic;

namespace RipcordSoftware.HttpWebClient
{
    public class HttpWebClientHeaders
    {
        #region Private fields
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Constructor
        public HttpWebClientHeaders()
        {
            Method = "GET";
            Uri = "/";
            Port = 80;
            Hostname = "localhost";
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
                _headers.TryGetValue(key, out value);
                return value;
            }

            set
            {
                if (value != null)
                {
                    _headers[key] = value;
                }
                else
                {
                    _headers.Remove(key);
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

            foreach (var pair in _headers)
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

