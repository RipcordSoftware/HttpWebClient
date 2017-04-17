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
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;

using Xunit;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClient.UnitTests
{
    [Collection("HttpWebClientContainer")]
    [ExcludeFromCodeCoverage]
    public class TestHttpWebClientRequest
    {
        #region Types
        public class TestSocket : IHttpWebClientSocket
        {
            private readonly MemoryStream _responseStream;
            private readonly MemoryStream _requestStream;
            private readonly StringBuilder _requestText;

            public TestSocket(string host, int port, int timeout, StringBuilder requestText, string responseText)
            {
                _requestText = requestText;
                _requestStream = new MemoryStream();
                _responseStream = new MemoryStream(Encoding.ASCII.GetBytes(responseText));
            }

            public bool Connected => true;

            public int Available => (int)(_responseStream.Length - _responseStream.Position);

            public int Timeout { get; set; }
            public bool NoDelay { get; set; }
            public bool ForceClose { protected get; set; }

            public IntPtr Handle { get { throw new NotImplementedException(); } }

            public void Close() { }

            public void Dispose() { }

            public void Flush() { }

            public void KeepAliveOnClose(int? timeout = default(int?)) { }

            public int Receive(byte[] buffer, int offset, int count, bool peek = false, SocketFlags flags = SocketFlags.None)
            {
                var read = _responseStream.Read(buffer, offset, count);
                if (read > 0 && peek)
                {
                    _responseStream.Position -= read;
                }

                return read;
            }

            public int Send(byte[] buffer, int offset, int count, SocketFlags flags = SocketFlags.None)
            {
                _requestText.Append(Encoding.ASCII.GetString(buffer, offset, count));

                _requestStream.Write(buffer, offset, count);
                return count;
            }

            public string RequestText { get { return _requestText.ToString(); } }
        }
        #endregion

        #region Constructor
        public TestHttpWebClientRequest()
        {
            HttpWebClientContainer.Clear();            
        }
        #endregion

        #region Tests
        [Fact]
        public void TestRequestHeadersFromUrl()
        {
            var request = new HttpWebClientRequest("http://localhost:42/uri");
            Assert.Equal("localhost", request.Hostname);
            Assert.Equal(42, request.Headers.Port);
            Assert.Equal("/uri", request.Headers.Uri);
            Assert.False(request.Headers.Secure);
            Assert.Equal("GET", request.Method);
            Assert.Equal("*/*", request.Accept);
            Assert.Equal("gzip, deflate", request.AcceptEncoding);
            Assert.Null(request.ContentEncoding);
            Assert.Equal(0, request.ContentLength);
            Assert.Null(request.ContentType);
            Assert.Null(request.TransferEncoding);
            Assert.Equal("Mozilla/5.0 RSHttpWebClient/1.0", request.UserAgent);
        }

        [Fact]
        public void TestRequestHeadersFromUrlAltConstructor()
        {
            var request = new HttpWebClientRequest("localhost", 42, "/uri");
            Assert.Equal("localhost", request.Hostname);
            Assert.Equal(42, request.Headers.Port);
            Assert.Equal("/uri", request.Headers.Uri);
            Assert.False(request.Headers.Secure);
            Assert.Equal("GET", request.Method);
            Assert.Equal("*/*", request.Accept);
            Assert.Equal("gzip, deflate", request.AcceptEncoding);
            Assert.Null(request.ContentEncoding);
            Assert.Equal(0, request.ContentLength);
            Assert.Null(request.ContentType);
            Assert.Null(request.TransferEncoding);
            Assert.Equal("Mozilla/5.0 RSHttpWebClient/1.0", request.UserAgent);
        }

        [Fact]
        public void TestRequestHeadersFromUrlAltConstructorNullUri()
        {
            var request = new HttpWebClientRequest("localhost", 42);
            Assert.Equal("localhost", request.Hostname);
            Assert.Equal(42, request.Headers.Port);
            Assert.Equal("/", request.Headers.Uri);
            Assert.False(request.Headers.Secure);
            Assert.Equal("GET", request.Method);
            Assert.Equal("*/*", request.Accept);
            Assert.Equal("gzip, deflate", request.AcceptEncoding);
            Assert.Null(request.ContentEncoding);
            Assert.Equal(0, request.ContentLength);
            Assert.Null(request.ContentType);
            Assert.Null(request.TransferEncoding);
            Assert.Equal("Mozilla/5.0 RSHttpWebClient/1.0", request.UserAgent);
        }

        [Fact]
        public void TestRequestHeadersFromUrlAltConstructorEmptyUri()
        {
            var request = new HttpWebClientRequest("localhost", 42, string.Empty);
            Assert.Equal("localhost", request.Hostname);
            Assert.Equal(42, request.Headers.Port);
            Assert.Equal("/", request.Headers.Uri);
            Assert.False(request.Headers.Secure);
            Assert.Equal("GET", request.Method);
            Assert.Equal("*/*", request.Accept);
            Assert.Equal("gzip, deflate", request.AcceptEncoding);
            Assert.Null(request.ContentEncoding);
            Assert.Equal(0, request.ContentLength);
            Assert.Null(request.ContentType);
            Assert.Null(request.TransferEncoding);
            Assert.Equal("Mozilla/5.0 RSHttpWebClient/1.0", request.UserAgent);
        }

        [Fact]
        public void TestRequestHeadersFromUrlAltConstructorMissingRootUri()
        {
            var request = new HttpWebClientRequest("localhost", 42, "myuri");
            Assert.Equal("localhost", request.Hostname);
            Assert.Equal(42, request.Headers.Port);
            Assert.Equal("/myuri", request.Headers.Uri);
            Assert.False(request.Headers.Secure);
            Assert.Equal("GET", request.Method);
            Assert.Equal("*/*", request.Accept);
            Assert.Equal("gzip, deflate", request.AcceptEncoding);
            Assert.Null(request.ContentEncoding);
            Assert.Equal(0, request.ContentLength);
            Assert.Null(request.ContentType);
            Assert.Null(request.TransferEncoding);
            Assert.Equal("Mozilla/5.0 RSHttpWebClient/1.0", request.UserAgent);
        }

        [Fact]
        public void TestRequestHeadersFromSecureUrl()
        {
            var request = new HttpWebClientRequest("https://www.test.com/hello/world");
            Assert.Equal("www.test.com", request.Hostname);
            Assert.Equal(443, request.Headers.Port);
            Assert.Equal("/hello/world", request.Headers.Uri);
            Assert.True(request.Headers.Secure);
            Assert.Equal("GET", request.Method);
            Assert.Equal("*/*", request.Accept);
            Assert.Equal("gzip, deflate", request.AcceptEncoding);
            Assert.Null(request.ContentEncoding);
            Assert.Equal(0, request.ContentLength);
            Assert.Null(request.ContentType);
            Assert.Null(request.TransferEncoding);
            Assert.Equal("Mozilla/5.0 RSHttpWebClient/1.0", request.UserAgent);
        }

        [Fact]
        public void TestSimpleRequestResponse()
        {
            var requestText = new StringBuilder();
            var responseText = "HTTP/1.1 200 OK\r\nContent-Length: 11\r\nContent-Type: text/ascii\r\nConnection: close\r\n\r\nhello world";

            HttpWebClientContainer.Register<IHttpWebClientSocket>((h, p, t) => { return new TestSocket((string)h, (int)p, (int)t, requestText, responseText); });

            var request = new HttpWebClientRequest("http://www.test.com/hello/world");
            using (var response = request.GetResponse())
            {
                Assert.Equal(200, response.StatusCode);
                Assert.Equal("OK", response.StatusDescription);
                Assert.Equal(11, response.ContentLength);
                Assert.Equal("text/ascii", response.ContentType);
                Assert.Null(response.TransferEncoding);
                Assert.Null(response.KeepAlive);
                Assert.Null(response.KeepAliveTimeout);
                Assert.Equal("close", response.Connection);

                using (var responseStream = response.GetResponseStream())
                {
                    using (var stream = new StreamReader(responseStream))
                    {
                        var body = stream.ReadToEnd();
                        Assert.Equal("hello world", body);
                    }
                }
            }

            Assert.True(requestText.Length > 0);
            Assert.Equal("GET /hello/world HTTP/1.1\r\nHost: www.test.com:80\r\nUser-Agent: Mozilla/5.0 RSHttpWebClient/1.0\r\nAccept: */*\r\nAccept-Encoding: gzip, deflate\r\n\r\n", requestText.ToString());
        }

        [Fact]
        public void TestSimplePutRequestResponse()
        {
            var requestText = new StringBuilder();
            var responseText = "HTTP/1.1 200 OK\r\nContent-Length: 11\r\nContent-Type: text/ascii\r\nConnection: close\r\n\r\nhello world";

            HttpWebClientContainer.Register<IHttpWebClientSocket>((h, p, t) => { return new TestSocket((string)h, (int)p, (int)t, requestText, responseText); });

            var request = new HttpWebClientRequest("http://www.test.com/");
            request.Method = "PUT";
            request.ContentType = "text/ascii";
            request.ContentLength = 33;

            using (var requestStream = request.GetRequestStream())
            {
                using (var stream = new StreamWriter(requestStream))
                {
                    stream.Write("hello world");
                    stream.Write("hello world");
                    stream.Flush();
                    stream.Write("hello world");
                }
            }

            using (var response = request.GetResponse())
            {
                Assert.Equal(200, response.StatusCode);
                Assert.Equal("OK", response.StatusDescription);
                Assert.Equal(11, response.ContentLength);
                Assert.Equal("text/ascii", response.ContentType);
                Assert.Null(response.TransferEncoding);
                Assert.Null(response.KeepAlive);
                Assert.Null(response.KeepAliveTimeout);
                Assert.Equal("close", response.Connection);

                using (var responseStream = response.GetResponseStream())
                {
                    using (var stream = new StreamReader(responseStream))
                    {
                        var body = stream.ReadToEnd();
                        Assert.Equal("hello world", body);
                    }
                }
            }

            Assert.True(requestText.Length > 0);
            Assert.Equal(
                "PUT / HTTP/1.1\r\nHost: www.test.com:80\r\n" +
                "User-Agent: Mozilla/5.0 RSHttpWebClient/1.0\r\nAccept: */*\r\nAccept-Encoding: gzip, deflate\r\n" +
                "Content-Type: text/ascii\r\nContent-Length: 33\r\n\r\n" +
                "hello worldhello worldhello world",
                requestText.ToString());
        }

        [Fact]
        public void TestSimpleRequestChunkedResponse()
        {
            var requestText = new StringBuilder();
            var responseText = "HTTP/1.1 200 OK\r\nContent-Type: text/ascii\r\nConnection: close\r\nTransfer-Encoding: chunked\r\n\r\nb\r\nhello world\r\n0\r\n\r\n";

            HttpWebClientContainer.Register<IHttpWebClientSocket>((h, p, t) => { return new TestSocket((string)h, (int)p, (int)t, requestText, responseText); });

            var request = new HttpWebClientRequest("http://www.test.com/hello/world");
            using (var response = request.GetResponse())
            {
                Assert.Equal(200, response.StatusCode);
                Assert.Equal("OK", response.StatusDescription);
                Assert.Equal(0, response.ContentLength);
                Assert.Equal("text/ascii", response.ContentType);
                Assert.Equal("chunked", response.TransferEncoding);
                Assert.Null(response.KeepAlive);
                Assert.Null(response.KeepAliveTimeout);
                Assert.Equal("close", response.Connection);

                using (var responseStream = response.GetResponseStream())
                {
                    using (var stream = new StreamReader(responseStream))
                    {
                        var body = stream.ReadToEnd();
                        Assert.Equal("hello world", body);
                    }
                }
            }

            Assert.True(requestText.Length > 0);
            Assert.Equal("GET /hello/world HTTP/1.1\r\nHost: www.test.com:80\r\nUser-Agent: Mozilla/5.0 RSHttpWebClient/1.0\r\nAccept: */*\r\nAccept-Encoding: gzip, deflate\r\n\r\n", requestText.ToString());
        }

        [Fact]
        public void TestSimplePutRequestChunkedResponse()
        {
            var requestText = new StringBuilder();
            var responseText = "HTTP/1.1 200 OK\r\nContent-Length: 11\r\nContent-Type: text/ascii\r\nConnection: close\r\n\r\nhello world";

            HttpWebClientContainer.Register<IHttpWebClientSocket>((h, p, t) => { return new TestSocket((string)h, (int)p, (int)t, requestText, responseText); });

            var request = new HttpWebClientRequest("http://www.test.com/");
            request.Method = "PUT";
            request.ContentType = "text/ascii";

            using (var requestStream = request.GetRequestStream())
            {
                using (var stream = new StreamWriter(requestStream))
                {
                    stream.Write("hello world");
                    stream.Write("hello world");
                    stream.Flush();
                    stream.Write("hello world");
                }
            }

            using (var response = request.GetResponse())
            {
                Assert.Equal(200, response.StatusCode);
                Assert.Equal("OK", response.StatusDescription);
                Assert.Equal(11, response.ContentLength);
                Assert.Equal("text/ascii", response.ContentType);
                Assert.Null(response.TransferEncoding);
                Assert.Null(response.KeepAlive);
                Assert.Null(response.KeepAliveTimeout);
                Assert.Equal("close", response.Connection);

                using (var responseStream = response.GetResponseStream())
                {
                    using (var stream = new StreamReader(responseStream))
                    {
                        var body = stream.ReadToEnd();
                        Assert.Equal("hello world", body);
                    }
                }
            }

            Assert.True(requestText.Length > 0);
            Assert.Equal(
                "PUT / HTTP/1.1\r\nHost: www.test.com:80\r\n" +
                "User-Agent: Mozilla/5.0 RSHttpWebClient/1.0\r\nAccept: */*\r\nAccept-Encoding: gzip, deflate\r\n" +
                "Content-Type: text/ascii\r\nTransfer-Encoding: chunked\r\n\r\n" +
                "16\r\nhello worldhello world\r\nB\r\nhello world\r\n0\r\n\r\n",
                requestText.ToString());
        }
        #endregion
    }
}
