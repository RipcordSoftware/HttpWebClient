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

using RipcordSoftware.HttpWebClient;


namespace HttpWebClient.UnitTests
{
    [Collection("HttpWebClientContainer")]
    [ExcludeFromCodeCoverage]
    public class TestHttpWebClientRequestStream
    {
        private readonly byte[] _buffer = new byte[16 * 1024];

        public TestHttpWebClientRequestStream()
        {
            for (var i = 0; i < _buffer.Length; ++i)
            {
                _buffer[i] = (byte)('a' + (i % 26));
            }
        }

        [Fact]
        public void TestInitializedRequestStream()
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
        public void TestRequestStreamWrite()
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

                Assert.True(socket.RequestText.StartsWith("abcdefghijklmnopqrstuvwxyz"));
            }
        }
    }
}
