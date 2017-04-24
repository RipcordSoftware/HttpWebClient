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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using Xunit;
using Moq;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClient.UnitTests
{
    [ExcludeFromCodeCoverage]
    public class TestHttpWebClientChunkedResponseStream
    {
        private const string TestChunkedText = "16\r\nhello worldhello world\r\nB\r\nhello world\r\n0\r\n\r\n";
        private static readonly byte[] _textChunkedBytes = Encoding.ASCII.GetBytes(TestChunkedText);

        [Fact]
        public void TestInitializedHttpWebClientChunkedResponseStream()
        {
            var socket = new MemoryStreamSocket();
            var memStream = new MemoryStream();

            using (var responseStream = new HttpWebClientResponseStream(socket, memStream))
            {
                using (var stream = new HttpWebClientChunkedResponseStream(responseStream))
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
        }

        [Fact]
        public void TestHttpWebClientChunkedResponseStreamRead()
        {
            var socket = new MemoryStreamSocket();
            var memStream = new MemoryStream(_textChunkedBytes);

            using (var responseStream = new HttpWebClientResponseStream(socket, memStream))
            {
                using (var stream = new HttpWebClientChunkedResponseStream(responseStream))
                {
                    Assert.Equal(0, stream.Length);
                    Assert.Equal(0, stream.Position);
                    Assert.Equal(_textChunkedBytes.Length, stream.Available);
                    Assert.Equal(0, stream.SocketAvailable);
                    Assert.Equal(_textChunkedBytes.Length, stream.BufferAvailable);
                    Assert.Equal(22, stream.Read(new byte[256], 0, 256));
                    Assert.Equal(11, stream.Read(new byte[256], 0, 256));
                    Assert.Equal(-1, stream.ReadByte());
                    Assert.Equal(0, stream.SocketReceive(new byte[256], 0, 256));
                }
            }
        }
    }
}
