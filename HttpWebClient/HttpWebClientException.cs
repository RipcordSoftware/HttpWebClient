using System;
using System.IO;

namespace RipcordSoftware.HttpWebClient
{
    [Serializable]
    public class HttpWebClientException : ApplicationException
    {
        public HttpWebClientException(string msg) : base(msg) {}
        public HttpWebClientException(string msg, Exception ex) : base(msg, ex) {}
    }

    [Serializable]
    public class HttpWebClientResponseException : HttpWebClientException
    {
        public HttpWebClientResponseException(string msg) : base(msg) {}
        public HttpWebClientResponseException(string msg, Exception ex) : base(msg, ex) {}
    }

    [Serializable]
    public class HttpWebClientResponseStatusException : HttpWebClientResponseException
    {
        public HttpWebClientResponseStatusException(string msg, int statusCode, string description, HttpWebClientHeaders headers, byte[] body) : base(msg)
        {
            StatusCode = statusCode;
            Description = description;
            Headers = headers;
            Body = body;
        }

        public HttpWebClientResponseStatusException(string msg, Exception ex, int statusCode, string description, HttpWebClientHeaders headers, byte[] body) : base(msg, ex)
        {
            StatusCode = statusCode;
            Description = description;
            Headers = headers;
            Body = body;
        }

        public int StatusCode { get; protected set; }
        public string Description { get; protected set; }
        public HttpWebClientHeaders Headers { get; protected set; }
        public byte[] Body { get; protected set; }
    }

    [Serializable]
    public class HttpWebClientRequestException : HttpWebClientException
    {
        public HttpWebClientRequestException(string msg) : base(msg) {}
        public HttpWebClientRequestException(string msg, Exception ex) : base(msg, ex) {}
    }
}

