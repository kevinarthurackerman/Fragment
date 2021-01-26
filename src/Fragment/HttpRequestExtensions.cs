using Microsoft.AspNetCore.Http;
using System;

namespace Fragment
{
    public static class HttpRequestExtensions
    {
        private const string RequestedWithHeader = "X-Requested-With";
        private const string XmlHttpRequest = "XMLHttpRequest";

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Headers != null)
                return request.Headers[RequestedWithHeader] == XmlHttpRequest;

            return false;
        }

        internal static string GetUrl(this HttpRequest httpRequest) =>
            String.Concat(
                httpRequest.Scheme,
                "://",
                httpRequest.Host.ToUriComponent(),
                httpRequest.PathBase.ToUriComponent(),
                httpRequest.Path.ToUriComponent(),
                httpRequest.QueryString.ToUriComponent());
    }
}
