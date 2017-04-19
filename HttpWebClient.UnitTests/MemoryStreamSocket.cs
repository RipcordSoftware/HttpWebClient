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
using System.Net.Sockets;
using System.Text;
using System.Diagnostics.CodeAnalysis;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClient.UnitTests
{
    [ExcludeFromCodeCoverage]
    internal class MemoryStreamSocket : IHttpWebClientSocket
    {
        #region Private fields
        private readonly MemoryStream _responseStream;
        private readonly MemoryStream _requestStream;
        private readonly StringBuilder _requestText;
        #endregion

        #region Constructor
        public MemoryStreamSocket() : this(new StringBuilder(), string.Empty)
        {
        }

        public MemoryStreamSocket(StringBuilder requestText, string responseText)
        {
            _requestText = requestText;
            _requestStream = new MemoryStream();
            _responseStream = new MemoryStream(Encoding.ASCII.GetBytes(responseText));
        }
        #endregion

        #region Public properties
        public bool Connected => true;

        public int Available { get { return (int)(_responseStream.Length - _responseStream.Position); } }

        public int Timeout { get; set; }
        public bool NoDelay { get; set; }
        public bool ForceClose { protected get; set; }

        public IntPtr Handle { get { throw new NotImplementedException(); } }

        public string RequestText { get { return _requestText.ToString(); } }
        public long Position { get { return _requestStream.Position;  } }
        public long Length { get { return _requestStream.Length; } }
        #endregion

        #region Public methods
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
        #endregion        
    }
}
