using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fragment
{
    public class FragmentedResult : IActionResult
    {
        private readonly bool _navigateToPage;
        private readonly IEnumerable<IActionResult> _fragments;

        public FragmentedResult(params IActionResult[] fragments) : this(false, fragments) { }

        public FragmentedResult(bool navigateToPage, params IActionResult[] fragments)
        {
            _navigateToPage = navigateToPage;
            _fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            // todo: detect when request is not ajax and return a single content for page navigation instead
            if (!context.HttpContext.Request.Headers.Any(x => x.Key == "X-Requested-With" && x.Value == "XMLHttpRequest"))
                throw new InvalidOperationException($"{nameof(FragmentedResult)} responses can only be returned for requests made with XMLHttpRequest");

            var boundary = Guid.NewGuid().ToString();
            context.HttpContext.Response.Headers.Add(HeaderNames.ContentType, $"multipart/byteranges; boundary={boundary}");

            if (_navigateToPage)
            {
                var setUrl = context.HttpContext.Request.GetUrl();
                context.HttpContext.Response.Headers.Add("X-Fragment-Url", setUrl);
            }

            var multipartFeatureCollection = new FeatureCollection();
            foreach (var feature in context.HttpContext.Features)
                multipartFeatureCollection[feature.Key] = feature.Value;

            var multipartContent = new MultipartContent("byteranges", boundary);

            foreach(var fragment in _fragments)
            {
                multipartFeatureCollection[typeof(IHttpResponseFeature)] = new HttpResponseFeature();
                multipartFeatureCollection[typeof(IHttpResponseBodyFeature)] = new StreamResponseBodyFeature(new MemoryStream());

                var multipartHttpContext = context.HttpContext.RequestServices
                    .GetRequiredService<IHttpContextFactory>().Create(multipartFeatureCollection);

                var multipartActionContext = new ActionContext(multipartHttpContext, context.RouteData, context.ActionDescriptor, context.ModelState);

                await fragment.ExecuteResultAsync(multipartActionContext);

                var body = multipartHttpContext.Response.Body;
                body.Seek(0, SeekOrigin.Begin);
                var bodyContent = new StreamContent(multipartHttpContext.Response.Body);
                foreach (var header in multipartHttpContext.Response.Headers)
                    bodyContent.Headers.Add(header.Key, (IEnumerable<string>)header.Value);
                multipartContent.Add(bodyContent);
            }

            var multipartStream = await multipartContent.ReadAsStreamAsync();

            await multipartStream.CopyToAsync(context.HttpContext.Response.Body);
        }
    }
}
