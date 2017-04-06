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
using System.Collections.Concurrent;

namespace RipcordSoftware.HttpWebClient
{
    internal static class HttpWebClientSocketCache
    {
        #region Private fields
        private static ConcurrentDictionary<string, ConcurrentQueue<HttpWebClientSocket.Socket>> _socketCache = new ConcurrentDictionary<string, ConcurrentQueue<HttpWebClientSocket.Socket>>();
        #endregion

        #region Public methods
        public static HttpWebClientSocket.Socket GetSocket(string hostname, int port, int timeout)
        {
            HttpWebClientSocket.Socket socket = null;
            var key = MakeKey(hostname, port);

            ConcurrentQueue<HttpWebClientSocket.Socket> socketQueue;
            if (_socketCache.TryGetValue(key, out socketQueue))
            {
                HttpWebClientSocket.Socket temp = null;
                while (socketQueue.Count > 0 && socketQueue.TryDequeue(out temp))
                {
                    if (!temp.IsKeepAliveExpired && temp.Connected)
                    {
                        socket = temp;
                        socket.Timeout = timeout;
                        break;
                    }

                    temp.Close();
                }
            }

            return socket;
        }

        public static void ReleaseSocket(HttpWebClientSocket.Socket socket)
        {
            bool released = false;

            if (socket.Connected)
            {
                var key = MakeKey(socket.Hostname, socket.Port);

                ConcurrentQueue<HttpWebClientSocket.Socket> socketQueue;
                if (!_socketCache.TryGetValue(key, out socketQueue))
                {
                    socketQueue = new ConcurrentQueue<HttpWebClientSocket.Socket>();
                    if (!_socketCache.TryAdd(key, socketQueue))
                    {
                        socketQueue = null;
                        _socketCache.TryGetValue(key, out socketQueue);
                    }
                }

                if (socketQueue != null)
                {
                    socketQueue.Enqueue(socket);
                    released = true;
                }
            }

            if (!released)
            {
                socket.Close();
            }                
        }
        #endregion

        #region Private methods
        private static string MakeKey(string hostname, int port)
        {
            return hostname + "~~~" + port.ToString();
        }
        #endregion
    }
}

