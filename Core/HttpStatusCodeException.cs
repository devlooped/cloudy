using System;
using System.Net;

namespace Cloudy
{
    public class HttpStatusCodeException : Exception
    {
        public HttpStatusCodeException(HttpStatusCode statusCode) => StatusCode = statusCode;

        public HttpStatusCode StatusCode { get; }
    }
}
