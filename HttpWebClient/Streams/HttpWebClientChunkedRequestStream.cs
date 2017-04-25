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

namespace RipcordSoftware.HttpWebClient
{
    internal sealed class HttpWebClientChunkedRequestStream : Stream
    {
        #region Constants
        internal const int MaxRequestChunkSize = 2048;
        private const string EndOfLine = "\r\n";
        #endregion

        #region Private fields
        private static readonly byte[] _maxBlockSizeHeader = GetChunkHeader(MaxRequestChunkSize);
        private static readonly byte[] _endResponseHeader = GetChunkHeader(0);
        private static readonly byte[] _endOfLineBytes = System.Text.Encoding.ASCII.GetBytes(EndOfLine);

        private readonly byte[] _streamBuffer;

        private Stream _stream = null;
        private long _position = 0;
        #endregion

        #region Constructor
        public HttpWebClientChunkedRequestStream(Stream stream)
        {
            this._stream = stream;

            _streamBuffer = new byte[_maxBlockSizeHeader.Length + MaxRequestChunkSize + _endOfLineBytes.Length];
        }
        #endregion

        #region Public methods
        public override void Write(byte[] buffer, int offset, int count)
        {
            var blocks = count / MaxRequestChunkSize;
            var overflow = count % MaxRequestChunkSize;

            if (blocks > 0)
            {
                // copy the chunk header into the stream buffer
                Array.Copy(_maxBlockSizeHeader, _streamBuffer, _maxBlockSizeHeader.Length);

                // copy the chunk trailer into the stream buffer
                Array.Copy(_endOfLineBytes, 0, _streamBuffer, _streamBuffer.Length - _endOfLineBytes.Length, _endOfLineBytes.Length);

                for (int i = 0; i < blocks; i++)
                {
                    // copy in the chunk data
                    Array.Copy(buffer, offset, _streamBuffer, _maxBlockSizeHeader.Length, MaxRequestChunkSize);
                    offset += MaxRequestChunkSize;

                    // write the buffer
                    _stream.Write(_streamBuffer, 0, _streamBuffer.Length);

                    _position += _streamBuffer.Length;
                }
            }

            if (overflow > 0)
            {
                // get the chunk overflow header
                var header = GetChunkHeader(overflow);

                // copy the header into the stream buffer
                Array.Copy(header, _streamBuffer, header.Length);
                int overflowLength = header.Length;

                // copy the chunk body
                Array.Copy(buffer, offset, _streamBuffer, overflowLength, overflow);
                overflowLength += overflow;

                // copy the chunk trailer
                Array.Copy(_endOfLineBytes, 0, _streamBuffer, overflowLength, _endOfLineBytes.Length);
                overflowLength += _endOfLineBytes.Length;

                // write the overflow data into the socket
                _stream.Write(_streamBuffer, 0, overflowLength);

                _position += overflowLength;
            }
        }

        public override void Close()
        {
            if (_stream != null)
            {
                // the response finishes with a \r\n
                _stream.Write(_endResponseHeader, 0, _endResponseHeader.Length);

                _stream.Close();
                _stream = null;
            }
        }
        #endregion

        #region Private methods
        private static byte[] GetChunkHeader(int size)
        {
            var format = "{0:X}" + EndOfLine + (size == 0 ? EndOfLine : string.Empty);
            var text = string.Format(format, size);
            return System.Text.Encoding.ASCII.GetBytes(text);
        }
        #endregion

        #region implemented abstract members of Stream
        public override void Flush()
        {
            if (_stream != null)
            {
                _stream.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }

        public override void SetLength(long value) { throw new NotImplementedException(); }

        public override bool CanTimeout { get { return _stream.CanTimeout; } }

        public override bool CanRead { get { return false; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return true; } }

        public override long Length { get { return _position; } }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
