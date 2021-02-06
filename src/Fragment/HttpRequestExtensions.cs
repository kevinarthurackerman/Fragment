using Microsoft.AspNetCore.Http;
using System;

namespace Fragment
{
    public static class HttpRequestExtensions
    {
        internal static bool IsFragmentedRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Headers != null)
                return request.Headers.TryGetValue("X-Fragmented", out var value)
                    && value.ToString().ToLower() != "false";

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
