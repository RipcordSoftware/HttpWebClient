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
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Xunit;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClient.UnitTests
{
    [ExcludeFromCodeCoverage]
    public class TestHttpWebClientResponseStream
    {
        #region Private fields
        private const string HeaderText =
            "HTTP/1.1 200 OK\r\n" +
            "Date: Mon, 23 May 2005 22:38:34 GMT\r\n" +
            "Content-Type: text/html; charset=UTF-8\r\n" +
            "Content-Encoding: UTF-8\r\n" +
            "Content-Length: 138\r\n" +
            "Last-Modified: Wed, 08 Jan 2003 23:11:55 GMT\r\n" +
            "Server: Apache/1.3.3.7 (Unix) (Red-Hat/Linux)\r\n" +
            "ETag: \"3f80f-1b6-3e1cb03b\"\r\n" +
            "Accept-Ranges: bytes\r\n" +
            "Connection: close\r\n" +
            "\r\n";

        private const string BodyText =            
            "<html>\r\n" +
            "<head>\r\n" +
            "  <title>An Example Page</title>\r\n" +
            "</head>\r\n" +
            "<body>\r\n" +
            "  Hello World, this is a very simple HTML document.\r\n" +
            "</body>\r\n" +
            "</html>";

        private const string FullResponseText = HeaderText + BodyText;

        private static readonly byte[] _headerBytes = Encoding.ASCII.GetBytes(HeaderText);
        private static readonly byte[] _bodyBytes = Encoding.ASCII.GetBytes(BodyText);
        private static readonly byte[] _fullResponseBytes = Encoding.ASCII.GetBytes(FullResponseText);
        #endregion

        #region Tests
        [Fact]
        public void TestInitializedHttpWebClientResponseStream()
        {
            var socket = new MemoryStreamSocket();
            var memStream = new MemoryStream();

            using (var stream = new HttpWebClientResponseStream(socket, memStream))
            {
                Assert.True(stream.CanRead);
                Assert.False(stream.CanWrite);
                Assert.False(stream.CanSeek);
                Assert.False(stream.CanTimeout);
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
                Assert.Equal(0, stream.Available);
                Assert.Equal(0, stream.SocketAvailable);
                Assert.Equal(0, stream.BufferAvailable);
                Assert.Equal(0, stream.Read(new byte[256], 0, 256));
                Assert.Equal(-1, stream.ReadByte());
                Assert.Equal(0, stream.SocketReceive(new byte[256], 0, 256));

                Assert.Throws<NotImplementedException>(() => stream.Seek(100, SeekOrigin.End));
                Assert.Throws<NotImplementedException>(() => stream.SetLength(1024));
                Assert.Throws<NotImplementedException>(() => stream.Write(new byte[256], 0, 256));
                Assert.Throws<NotImplementedException>(() => { stream.Position = 1024; });
            }
        }

        [Fact]
        public void TestHttpWebClientResponseStreamMemStreamRead()
        {
            var socket = new MemoryStreamSocket();
            var memStream = new MemoryStream(_fullResponseBytes);

            Assert.False(socket.ForceClose);

            using (var stream = new HttpWebClientResponseStream(socket, memStream))
            {
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
                Assert.Equal(_fullResponseBytes.Length, stream.Available);
                Assert.Equal(0, stream.SocketAvailable);
                Assert.Equal(_fullResponseBytes.Length, stream.BufferAvailable);
                Assert.Equal(256, stream.Read(new byte[256], 0, 256));
                Assert.True(stream.ReadByte() >= 0);
                Assert.Equal(0, stream.SocketReceive(new byte[256], 0, 256));

                stream.SocketForceClose = true;
            }

            Assert.True(socket.ForceClose);
        }

        [Fact]
        public void TestHttpWebClientResponseStreamMemStreamSetLengthThenRead()
        {
            var memStream = new MemoryStream(_fullResponseBytes);

            using (var stream = new HttpWebClientResponseStream(null, memStream, _fullResponseBytes.Length))
            {
                Assert.Equal(_fullResponseBytes.Length, stream.Length);
                Assert.Equal(0, stream.Position);
                Assert.Equal(_fullResponseBytes.Length, stream.Available);
                Assert.Equal(0, stream.SocketAvailable);
                Assert.Equal(_fullResponseBytes.Length, stream.BufferAvailable);
                Assert.Equal(256, stream.Read(new byte[256], 0, 256));
                Assert.True(stream.ReadByte() >= 0);
                Assert.Equal(0, stream.SocketReceive(new byte[256], 0, 256));
            }
        }

        [Fact]
        public void TestHttpWebClientResponseStreamMemStreamReadToEnd()
        {
            var memStream = new MemoryStream(_fullResponseBytes);

            using (var stream = new HttpWebClientResponseStream(null, memStream))
            {
                var buffer = new byte[256];

                var response = new List<byte>();
                var bytesRead = 0;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    response.AddRange(new ArraySegment<byte>(buffer, 0, bytesRead));
                }

                Assert.Equal(_fullResponseBytes.Length, response.Count);
                Assert.True(response.SequenceEqual(_fullResponseBytes));
            }
        }

        [Fact]
        public void TestHttpWebClientResponseStreamSocketRead()
        {
            var socket = new MemoryStreamSocket(null, FullResponseText);

            using (var stream = new HttpWebClientResponseStream(socket, null))
            {
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
                Assert.Equal(_fullResponseBytes.Length, stream.Available);
                Assert.Equal(_fullResponseBytes.Length, stream.SocketAvailable);
                Assert.Equal(0, stream.BufferAvailable);
                Assert.Equal(64, stream.Read(new byte[64], 0, 64));
                Assert.True(stream.ReadByte() >= 0);
                Assert.Equal(64, stream.SocketReceive(new byte[64], 0, 64));
            }
        }

        [Fact]
        public void TestHttpWebClientResponseStreamSplitRead()
        {
            var socket = new MemoryStreamSocket(null, BodyText);
            var memStream = new MemoryStream(_headerBytes);

            using (var stream = new HttpWebClientResponseStream(socket, memStream))
            {
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
                Assert.Equal(_fullResponseBytes.Length, stream.Available);
                Assert.Equal(_bodyBytes.Length, stream.SocketAvailable);
                Assert.Equal(_headerBytes.Length, stream.BufferAvailable);
                Assert.Equal(64, stream.Read(new byte[64], 0, 64));
                Assert.True(stream.ReadByte() >= 0);
                Assert.Equal(64, stream.SocketReceive(new byte[64], 0, 64));

                Assert.Equal(65, stream.Length);
                Assert.Equal(65, stream.Position);
            }
        }

        [Fact]
        public void TestHttpWebClientResponseStreamMemStreamPeek()
        {
            var socket = new MemoryStreamSocket();
            var memStream = new MemoryStream(_fullResponseBytes);

            using (var stream = new HttpWebClientResponseStream(socket, memStream))
            {
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
                Assert.Equal(_fullResponseBytes.Length, stream.Available);
                Assert.Equal(0, stream.SocketAvailable);
                Assert.Equal(_fullResponseBytes.Length, stream.BufferAvailable);

                Assert.Equal(256, stream.Read(new byte[256], 0, 256, true));

                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
                Assert.Equal(_fullResponseBytes.Length, stream.Available);
                Assert.Equal(0, stream.SocketAvailable);
                Assert.Equal(_fullResponseBytes.Length, stream.BufferAvailable);

                Assert.Equal(256, stream.Read(new byte[256], 0, 256));

                Assert.Equal(256, stream.Length);
                Assert.Equal(256, stream.Position);
                Assert.Equal(_fullResponseBytes.Length - 256, stream.Available);
                Assert.Equal(0, stream.SocketAvailable);
                Assert.Equal(_fullResponseBytes.Length - 256, stream.BufferAvailable);
            }
        }
        #endregion
    }
}
