using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Fragment
{
    internal class MultipartContent
    {
        public string ContentType { get; }
        public Stream Stream { get; }
        public HttpHeaders HttpHeaders { get; }

        internal MultipartContent(string contentType, Stream stream, HttpHeaders httpHeaders)
        {
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            HttpHeaders = httpHeaders ?? throw new ArgumentNullException(nameof(httpHeaders));
        }
    }

    internal class MultipartResult : Collection<MultipartContent>, IActionResult
    {
        private const string _subtype = "byteranges";

        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var content = new System.Net.Http.MultipartContent(_subtype);

            foreach (var item in this)
            {
                var streamContent = new StreamContent(item.Stream);

                streamContent.Headers.ContentType = new MediaTypeHeaderValue(item.ContentType);

                foreach (var header in item.HttpHeaders)
                    streamContent.Headers.Add(header.Key, header.Value);

                content.Add(streamContent);
            }

            context.HttpContext.Response.ContentLength = content.Headers.ContentLength;
            context.HttpContext.Response.ContentType = content.Headers.ContentType.ToString();

            await content.CopyToAsync(context.HttpContext.Response.Body);
        }
    }

    internal class HttpHeadersCollection : HttpHeaders { }
}
