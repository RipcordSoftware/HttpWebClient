using System;
using System.Collections.Concurrent;

namespace RipcordSoftware.HttpWebClient
{
    internal static class HttpWebClientSocketCache
    {
        #region Private fields
        private static ConcurrentDictionary<string, ConcurrentQueue<HttpWebClientSocket.Socket>> socketCache = new ConcurrentDictionary<string, ConcurrentQueue<HttpWebClientSocket.Socket>>();
        #endregion

        #region Public methods
        public static HttpWebClientSocket.Socket GetSocket(string hostname, int port, int timeout)
        {
            HttpWebClientSocket.Socket socket = null;
            var key = MakeKey(hostname, port);

            ConcurrentQueue<HttpWebClientSocket.Socket> socketQueue;
            if (socketCache.TryGetValue(key, out socketQueue))
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
                if (!socketCache.TryGetValue(key, out socketQueue))
                {
                    socketQueue = new ConcurrentQueue<HttpWebClientSocket.Socket>();
                    if (!socketCache.TryAdd(key, socketQueue))
                    {
                        socketQueue = null;
                        socketCache.TryGetValue(key, out socketQueue);
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

