using System;

namespace RipcordSoftware.HttpWebClient
{
    /// <summary>
    /// A non-exhaustive list of HTTP status codes
    /// </summary>
    public enum HttpWebClientStatusCode : int
    {
        OK = 200,
        Created = 201,
        Accepted = 202,

        MovedPermanently = 301,
        Found = 302,
        NotModified = 304,
        TemporaryRedirect = 307,
        PermanentRedirect = 308,

        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        Conflict = 409,
        PreconditionFailed = 412,

        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503
    }
}

