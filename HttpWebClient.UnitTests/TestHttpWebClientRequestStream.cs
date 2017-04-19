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
using System.Diagnostics.CodeAnalysis;

using Xunit;
using Moq;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClient.UnitTests
{
    [Collection("HttpWebClientContainer")]
    [ExcludeFromCodeCoverage]
    public class TestHttpWebClientRequestStream
    {
        #region Private fields
        private readonly byte[] _buffer = new byte[16 * 1024];
        #endregion

        #region Types
        private class ChunkedRequestInfo
        {
            public ChunkedRequestInfo(int chunks, int written, int padding)
            {
                Chunks = chunks;
                RawWritten = written;
                Padding = padding;
            }

            public int Chunks { get; protected set; }
            public int RawWritten { get; protected set; }
            public int ChunkedWritten { get { return Padding + RawWritten; } }
            public int Padding { get; protected set; }
            public int TotalPadding { get { return Padding + 5; } }
            public int TotalWritten { get { return RawWritten + TotalPadding; } }
        }
        #endregion

        #region Constructor
        public TestHttpWebClientRequestStream()
        {
            for (var i = 0; i < _buffer.Length; ++i)
            {
                _buffer[i] = (byte)('a' + (i % 26));
            }
        }
        #endregion

        #region Tests
        [Fact]
        public void TestInitializedInnerRequestStream()
        {
            using (var socket = new MemoryStreamSocket())
            {
                using (var stream = new HttpWebClientRequestStream.RequestStream(socket))
                {
                    Assert.False(stream.CanRead);
                    Assert.True(stream.CanWrite);
                    Assert.False(stream.CanSeek);
                    Assert.False(stream.CanTimeout);
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);

                    Assert.Throws<NotImplementedException>(() => stream.Seek(100, SeekOrigin.End));
                    Assert.Throws<NotImplementedException>(() => stream.ReadByte());
                    Assert.Throws<NotImplementedException>(() => stream.SetLength(1024));
                    Assert.Throws<NotImplementedException>(() => stream.Read(new byte[256], 0, 256));
                    Assert.Throws<NotImplementedException>(() => { stream.Position = 1024; });
                }
            }
        }

        [Fact]
        public void TestInnerRequestStreamWrite()
        {
            using (var socket = new MemoryStreamSocket())
            {
                using (var stream = new HttpWebClientRequestStream.RequestStream(socket))
                {
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);

                    stream.Write(_buffer, 0, _buffer.Length);

                    Assert.Equal(_buffer.Length, stream.Length);
                    Assert.Equal(_buffer.Length, stream.Position);                    
                }

                Assert.Equal(_buffer.Length, socket.RequestText.Length);
                Assert.True(socket.RequestText.StartsWith("abcdefghijklmnopqrstuvwxyz"));                
            }
        }

        [Fact]
        public void TestInnerRequestStreamFailedWrite()
        {
            var socket = new Mock<IHttpWebClientSocket>();
            socket.Setup(s => s.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<System.Net.Sockets.SocketFlags>())).Returns(0);

            using (var stream = new HttpWebClientRequestStream.RequestStream(socket.Object))
            {
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);

                var ex = Assert.Throws<HttpWebClientRequestException>(() => stream.Write(_buffer, 0, _buffer.Length));
                Assert.Null(ex.InnerException);

                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
            }
        }

        [Fact]
        public void TestInnerRequestStreamWriteThrows()
        {
            var socket = new Mock<IHttpWebClientSocket>();
            socket.Setup(s => s.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<System.Net.Sockets.SocketFlags>())).Throws<Exception>();

            using (var stream = new HttpWebClientRequestStream.RequestStream(socket.Object))
            {
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);

                var ex = Assert.Throws<HttpWebClientRequestException>(() => stream.Write(_buffer, 0, _buffer.Length));
                Assert.NotNull(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException);

                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
            }
        }

        [Fact]
        public void TestInitializedChunkedRequestStream()
        {
            using (var stream = new HttpWebClientRequestStream.ChunkedRequestStream(new MemoryStream()))
            {
                Assert.False(stream.CanRead);
                Assert.True(stream.CanWrite);
                Assert.False(stream.CanSeek);
                Assert.False(stream.CanTimeout);
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);

                Assert.Throws<NotImplementedException>(() => stream.Seek(100, SeekOrigin.End));
                Assert.Throws<NotImplementedException>(() => stream.ReadByte());
                Assert.Throws<NotImplementedException>(() => stream.SetLength(1024));
                Assert.Throws<NotImplementedException>(() => stream.Read(new byte[256], 0, 256));
                Assert.Throws<NotImplementedException>(() => { stream.Position = 1024; });
            }
        }

        [Fact]
        public void TestChunkedRequestStreamSimpleWrite()
        {
            var memStream = new NonDisposibleStream(new MemoryStream());
            var position = 0;

            using (var stream = new HttpWebClientRequestStream.ChunkedRequestStream(memStream))
            {
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);

                var buffer = new byte[] { 1, 2, 3, 4, 5 };
                stream.Write(buffer, 0, buffer.Length);
                position += 1 + 2 + 5 + 2;

                Assert.Equal(position, stream.Position);
                Assert.Equal(position, stream.Length);

                buffer = new byte[] { 6, 7, 8, 9, 0 };
                stream.Write(buffer, 0, buffer.Length);
                position += 1 + 2 + 5 + 2;

                Assert.Equal(position, stream.Position);
                Assert.Equal(position, stream.Length);

                buffer = new byte[] { 0x0a, 0x0b, 0x0c, 0x0d, 0x0e };
                stream.Write(buffer, 0, buffer.Length);
                position += 1 + 2 + 5 + 2;

                Assert.Equal(position, stream.Position);
                Assert.Equal(position, stream.Length);
            }

            position += 5;
            Assert.Equal(position, memStream.Position);
            Assert.Equal(position, memStream.Length);
        }

        [Fact]
        public void TestChunkedRequestStreamWrite()
        {
            var chunkInfo = CalculateChunkInfo(_buffer.Length);
            var memStream = new NonDisposibleStream(new MemoryStream());

            using (var stream = new HttpWebClientRequestStream.ChunkedRequestStream(memStream))
            {
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);

                stream.Write(_buffer, 0, _buffer.Length);                

                Assert.Equal(chunkInfo.ChunkedWritten, stream.Length);
                Assert.Equal(chunkInfo.ChunkedWritten, stream.Position);
            }

            Assert.Equal(chunkInfo.TotalWritten, memStream.Length);
            Assert.Equal(chunkInfo.TotalWritten, memStream.Position);
        }

        [Fact]
        public void TestInitializedRequestStream()
        {
            using (var socket = new MemoryStreamSocket())
            {
                var headers = new HttpWebClientHeaders();

                using (var stream = new HttpWebClientRequestStream(socket, headers))
                {
                    Assert.False(stream.CanRead);
                    Assert.True(stream.CanWrite);
                    Assert.False(stream.CanSeek);
                    Assert.False(stream.CanTimeout);
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);

                    Assert.Throws<NotImplementedException>(() => stream.Seek(100, SeekOrigin.End));
                    Assert.Throws<NotImplementedException>(() => stream.ReadByte());
                    Assert.Throws<NotImplementedException>(() => stream.SetLength(1024));
                    Assert.Throws<NotImplementedException>(() => stream.Read(new byte[256], 0, 256));
                    Assert.Throws<NotImplementedException>(() => { stream.Position = 1024; });
                }
            }
        }

        [Fact]
        public void TestRequestStreamWriteChunked()
        {
            var expectedHeaders = "POST /uri HTTP/1.1\r\nHost: localhost:42\r\nContent-Type: text/ascii\r\nTransfer-Encoding: chunked\r\n\r\n";
            var chunkInfo = CalculateChunkInfo(_buffer.Length);

            using (var socket = new MemoryStreamSocket())
            {
                var headers = new HttpWebClientHeaders();
                headers.Hostname = "localhost";
                headers.Port = 42;
                headers.Method = "POST";
                headers.Secure = false;
                headers.Uri = "/uri";
                headers["Content-Type"] = "text/ascii";

                using (var stream = new HttpWebClientRequestStream(socket, headers))
                {
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);

                    stream.Write(_buffer, 0, _buffer.Length);
                    
                    Assert.True(socket.RequestText.StartsWith(expectedHeaders));

                    Assert.Equal(chunkInfo.ChunkedWritten, stream.Length - expectedHeaders.Length);
                    Assert.Equal(chunkInfo.ChunkedWritten, stream.Position - expectedHeaders.Length);
                }

                Assert.Equal(chunkInfo.TotalWritten, socket.Length - expectedHeaders.Length);
                Assert.Equal(chunkInfo.TotalWritten, socket.Position - expectedHeaders.Length);
            }
        }

        [Fact]
        public void TestRequestStreamWriteLength()
        {
            var expectedHeaders =
                string.Format("POST /uri HTTP/1.1\r\nHost: localhost:42\r\nContent-Type: text/ascii\r\nContent-Length: {0}\r\n\r\n", _buffer.Length);

            using (var socket = new MemoryStreamSocket())
            {
                var headers = new HttpWebClientHeaders();
                headers.Hostname = "localhost";
                headers.Port = 42;
                headers.Method = "POST";
                headers.Secure = false;
                headers.Uri = "/uri";
                headers.ContentLength = _buffer.Length;
                headers["Content-Type"] = "text/ascii";

                using (var stream = new HttpWebClientRequestStream(socket, headers))
                {
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);

                    stream.Write(_buffer, 0, _buffer.Length);

                    Assert.True(socket.RequestText.StartsWith(expectedHeaders));

                    Assert.Equal(_buffer.Length + expectedHeaders.Length, stream.Length);
                    Assert.Equal(_buffer.Length + expectedHeaders.Length, stream.Position);
                }

                Assert.Equal(_buffer.Length + expectedHeaders.Length, socket.Length);
                Assert.Equal(_buffer.Length + expectedHeaders.Length, socket.Position);
            }
        }

        [Fact]
        public void TestRequestStreamWritePostEmptyBody()
        {
            var expectedHeaders =
                string.Format("POST /uri HTTP/1.1\r\nHost: localhost:42\r\nContent-Type: text/ascii\r\nContent-Length: 0\r\n\r\n", _buffer.Length);

            using (var socket = new MemoryStreamSocket())
            {
                var headers = new HttpWebClientHeaders();
                headers.Hostname = "localhost";
                headers.Port = 42;
                headers.Method = "POST";
                headers.Secure = false;
                headers.Uri = "/uri";
                headers.ContentLength = _buffer.Length;
                headers["Content-Type"] = "text/ascii";

                using (var stream = new HttpWebClientRequestStream(socket, headers))
                {
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);
                }

                Assert.True(socket.RequestText.StartsWith(expectedHeaders));
                Assert.Equal(expectedHeaders.Length, socket.Length);
                Assert.Equal(expectedHeaders.Length, socket.Position);
            }
        }

        [Fact]
        public void TestRequestStreamWritePutEmptyBody()
        {
            var expectedHeaders =
                string.Format("PUT /uri HTTP/1.1\r\nHost: localhost:42\r\nContent-Type: text/ascii\r\nContent-Length: 0\r\n\r\n", _buffer.Length);

            using (var socket = new MemoryStreamSocket())
            {
                var headers = new HttpWebClientHeaders();
                headers.Hostname = "localhost";
                headers.Port = 42;
                headers.Method = "PUT";
                headers.Secure = false;
                headers.Uri = "/uri";
                headers.ContentLength = _buffer.Length;
                headers["Content-Type"] = "text/ascii";

                using (var stream = new HttpWebClientRequestStream(socket, headers))
                {
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);
                }

                Assert.True(socket.RequestText.StartsWith(expectedHeaders));
                Assert.Equal(expectedHeaders.Length, socket.Length);
                Assert.Equal(expectedHeaders.Length, socket.Position);
            }
        }

        [Fact]
        public void TestRequestStreamFailedHeaderWrite()
        {
            var socket = new Mock<IHttpWebClientSocket>();
            socket.Setup(s => s.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<System.Net.Sockets.SocketFlags>())).Returns(0);

            var headers = new HttpWebClientHeaders();
            headers.Hostname = "localhost";
            headers.Port = 42;
            headers.Method = "PUT";
            headers.Secure = false;
            headers.Uri = "/uri";
            headers.ContentLength = _buffer.Length;
            headers["Content-Type"] = "text/ascii";

            Assert.Throws<HttpWebClientRequestException>(() =>
            {
                using (var stream = new HttpWebClientRequestStream(socket.Object, headers))
                {
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);
                }
            });
        }

        [Fact]
        public void TestRequestStreamCloseThrows()
        {
            var socket = new Mock<IHttpWebClientSocket>();
            socket.Setup(s => s.Send(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<System.Net.Sockets.SocketFlags>())).Throws<Exception>();

            var headers = new HttpWebClientHeaders();
            headers.Hostname = "localhost";
            headers.Port = 42;
            headers.Method = "PUT";
            headers.Secure = false;
            headers.Uri = "/uri";
            headers.ContentLength = _buffer.Length;
            headers["Content-Type"] = "text/ascii";

            Assert.Throws<HttpWebClientRequestException>(() =>
            {
                using (var stream = new HttpWebClientRequestStream(socket.Object, headers))
                {
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);
                }
            });
        }

        [Fact]
        public void TestRequestStreamHeaderWriteThrows()
        {
            var socket = new Mock<Stream>();
            socket.Setup(s => s.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Throws<Exception>();

            Assert.Throws<HttpWebClientRequestException>(() =>
            {
                using (var stream = new HttpWebClientRequestStream(socket.Object, new HttpWebClientHeaders()))
                {
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);
                }
            });
        }
        #endregion

        #region Private methods
        private static ChunkedRequestInfo CalculateChunkInfo(int written)
        {
            var chunks = written / HttpWebClientRequestStream.ChunkedRequestStream.MaxRequestChunkSize;
            var padding = chunks * (HttpWebClientRequestStream.ChunkedRequestStream.MaxRequestChunkSize.ToString("X").Length + 4);

            var lastChunkSize = written % HttpWebClientRequestStream.ChunkedRequestStream.MaxRequestChunkSize;
            chunks += lastChunkSize > 0 ? 1 : 0;

            padding += lastChunkSize > 0 ? lastChunkSize.ToString("X").Length + 2 : 0;

            return new ChunkedRequestInfo(chunks, written, padding);
        }
        #endregion
    }
}
