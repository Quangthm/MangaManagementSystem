using System.Net;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class ApiClientException
        : InvalidOperationException
    {
        public ApiClientException(
            string code,
            string message,
            HttpStatusCode statusCode,
            string requestMethod = "UNKNOWN",
            string requestUri = "UNKNOWN")
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
            RequestMethod = requestMethod;
            RequestUri = requestUri;
        }

        public string Code { get; }

        public HttpStatusCode StatusCode { get; }

        public string RequestMethod { get; }

        public string RequestUri { get; }
    }
}
