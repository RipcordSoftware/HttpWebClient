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
using System.Linq;
using System.Diagnostics.CodeAnalysis;

using Xunit;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClient.UnitTests
{
    [ExcludeFromCodeCoverage]
    public class TestHttpWebClientHeaders
    {
        [Fact]
        public void TestDefaultState()
        {
            var expectedHeaderText = "GET / HTTP/1.1\r\nHost: localhost:80\r\n\r\n";

            var headers = new HttpWebClientHeaders();
            Assert.Equal("GET", headers.Method);
            Assert.Equal("localhost", headers.Hostname);
            Assert.Equal(80, headers.Port);
            Assert.Equal("/", headers.Uri);
            Assert.False(headers.Secure);
            Assert.Null(headers.ContentLength);
            Assert.Equal(expectedHeaderText, headers.GetHeaders());
            Assert.True(Encoding.ASCII.GetBytes(expectedHeaderText).SequenceEqual(headers.GetHeaderBytes()));
        }

        [Fact]
        public void TestNonDefaultState()
        {
            var expectedHeaderText = "POST /someuri HTTP/1.1\r\nHost: somehost:8080\r\nContent-Length: 1024\r\n\r\n";

            var headers = new HttpWebClientHeaders();
            headers.Method = "POST";
            headers.Hostname = "somehost";
            headers.Port = 8080;
            headers.Uri = "/someuri";
            headers.ContentLength = 1024;

            Assert.Equal("POST", headers.Method);
            Assert.Equal("somehost", headers.Hostname);
            Assert.Equal(8080, headers.Port);
            Assert.Equal("/someuri", headers.Uri);
            Assert.False(headers.Secure);
            Assert.Equal(1024, headers.ContentLength.Value);
            Assert.Equal(expectedHeaderText, headers.GetHeaders());
            Assert.True(Encoding.ASCII.GetBytes(expectedHeaderText).SequenceEqual(headers.GetHeaderBytes()));
        }

        [Fact]
        public void TestNonDefaultByIndexerState()
        {
            var expectedHeaderText1 = "GET / HTTP/1.1\r\nHost: localhost:80\r\nMy-Header: somevalue\r\n\r\n";
            var expectedHeaderText2 = "GET / HTTP/1.1\r\nHost: localhost:80\r\n\r\n";
            var expectedHeaderText3 = "GET / HTTP/1.1\r\nHost: localhost:80\r\nMy-Header1: somevalue1\r\nMy-Header2: somevalue2\r\n\r\n";
            var expectedHeaderText4 = "GET / HTTP/1.1\r\nHost: localhost:80\r\nMy-Header2: somevalue2\r\n\r\n";
            var expectedHeaderText5 = "GET / HTTP/1.1\r\nHost: localhost:80\r\nMy-Header2: updatedsomevalue2\r\n\r\n";

            var headers = new HttpWebClientHeaders();
            headers["My-Header"] = "somevalue";

            Assert.Equal("GET", headers.Method);
            Assert.Equal("localhost", headers.Hostname);
            Assert.Equal(80, headers.Port);
            Assert.Equal("/", headers.Uri);
            Assert.False(headers.Secure);
            Assert.Null(headers.ContentLength);
            Assert.Equal("somevalue", headers["My-Header"]);
            Assert.Equal(expectedHeaderText1, headers.GetHeaders());
            Assert.True(Encoding.ASCII.GetBytes(expectedHeaderText1).SequenceEqual(headers.GetHeaderBytes()));

            headers["My-Header"] = null;
            Assert.Null(headers["My-Header"]);
            Assert.Equal(expectedHeaderText2, headers.GetHeaders());
            Assert.True(Encoding.ASCII.GetBytes(expectedHeaderText2).SequenceEqual(headers.GetHeaderBytes()));

            headers["My-Header1"] = "somevalue1";
            headers["My-Header2"] = "somevalue2";
            Assert.Equal(expectedHeaderText3, headers.GetHeaders());
            Assert.True(Encoding.ASCII.GetBytes(expectedHeaderText3).SequenceEqual(headers.GetHeaderBytes()));

            headers["My-Header1"] = null;
            Assert.Equal(expectedHeaderText4, headers.GetHeaders());
            Assert.True(Encoding.ASCII.GetBytes(expectedHeaderText4).SequenceEqual(headers.GetHeaderBytes()));

            headers["My-Header2"] = "updatedsomevalue2";
            Assert.Equal(expectedHeaderText5, headers.GetHeaders());
            Assert.True(Encoding.ASCII.GetBytes(expectedHeaderText5).SequenceEqual(headers.GetHeaderBytes()));
        }

    }
}
