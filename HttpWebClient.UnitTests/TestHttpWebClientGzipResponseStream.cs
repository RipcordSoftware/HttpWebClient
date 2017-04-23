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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

using Xunit;
using Ionic.Zlib;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClient.UnitTests
{
    [ExcludeFromCodeCoverage]
    public class TestHttpWebClientGzipResponseStream
    {
        #region Private fields
        private const string TestText = 
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Ut dui sem, dapibus sagittis ultricies sed, laoreet ac massa. Nulla molestie mi vel ex ultricies ullamcorper. " +
            "Sed sed lectus lacus. Nullam ullamcorper, eros a bibendum finibus, diam mauris iaculis nisl, at efficitur diam velit id orci. Integer interdum lobortis turpis non sodales. " +
            "Ut quis massa sed libero laoreet pretium auctor sit amet neque. Nam sed lacinia magna. Quisque venenatis congue lorem eget blandit. Orci varius natoque penatibus et magnis dis " +
            "parturient montes, nascetur ridiculus mus. Nunc magna sem, efficitur a auctor nec, consequat malesuada lacus. Quisque sed massa ac purus imperdiet consequat. Curabitur a " +
            "ligula non ipsum luctus elementum.";

        private static readonly byte[] _testBytes = Encoding.ASCII.GetBytes(TestText);
        #endregion

        #region Test
        [Fact]
        public void TestInitializedGzipResponseStream()
        {
            using (var stream = new HttpWebClientGZipResponseStream(new MemoryStream()))
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
        public void TestHttpWebClientGzipResponse()
        {
            var memStream = new NonDisposibleStream(new MemoryStream());

            using (var gzipStream = new GZipStream(memStream, CompressionMode.Compress))
            {
                gzipStream.Write(_testBytes, 0, _testBytes.Length);
            }

            memStream.Position = 0;

            var buffer = new byte[1024];
            var response = new List<byte>();
            using (var stream = new HttpWebClientGZipResponseStream(memStream))
            {
                var bytesRead = 0;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    response.AddRange(new ArraySegment<byte>(buffer, 0, bytesRead));
                }
            }

            Assert.Equal(_testBytes.Length, response.Count);
            Assert.True(_testBytes.SequenceEqual(response));
        }

        [Fact]
        public void TestInitializedDeflateResponseStream()
        {
            using (var stream = new HttpWebClientDeflateResponseStream(new MemoryStream()))
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
        public void TestHttpWebClientDeflateResponse()
        {
            var memStream = new NonDisposibleStream(new MemoryStream());

            using (var gzipStream = new DeflateStream(memStream, CompressionMode.Compress))
            {
                gzipStream.Write(_testBytes, 0, _testBytes.Length);
            }

            memStream.Position = 0;

            var buffer = new byte[1024];
            var response = new List<byte>();
            using (var stream = new HttpWebClientDeflateResponseStream(memStream))
            {
                var bytesRead = 0;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    response.AddRange(new ArraySegment<byte>(buffer, 0, bytesRead));
                }
            }

            Assert.Equal(_testBytes.Length, response.Count);
            Assert.True(_testBytes.SequenceEqual(response));
        }
        #endregion
    }
}
