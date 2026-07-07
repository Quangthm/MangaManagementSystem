using System.Net;

namespace MangaManagementSystem.Web.Services.Api
{
    public sealed class ApiClientException
        : InvalidOperationException
    {
        public ApiClientException(
            string code,
            string message,
            HttpStatusCode statusCode)
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }

        public string Code { get; }

        public HttpStatusCode StatusCode { get; }
    }
}
