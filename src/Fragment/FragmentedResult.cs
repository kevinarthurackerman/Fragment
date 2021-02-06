using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fragment
{
    public partial class FragmentedResult : IActionResult
    {
        public Uri PageUri { get; set; }
        public IActionResult PageFragment { get; set; }
        public IList<IActionResult> ViewFragments { get; } = new List<IActionResult>();

        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (!context.HttpContext.Request.IsFragmentedRequest())
            {
                await PageFragment.ExecuteResultAsync(context);
                return;
            }
            
            var boundary = Guid.NewGuid().ToString();
            context.HttpContext.Response.Headers.Add(HeaderNames.ContentType, $"multipart/byteranges; boundary={boundary}");

            if (PageUri != default)
            {
                var pageUri = PageUri.IsAbsoluteUri
                    ? PageUri
                    : new Uri(new Uri(context.HttpContext.Request.GetUrl()), PageUri.ToString());

                context.HttpContext.Response.Headers.Add("X-Fragment-Url", pageUri.AbsoluteUri);
            }

            var multipartFeatureCollection = new FeatureCollection();
            foreach (var feature in context.HttpContext.Features)
                multipartFeatureCollection[feature.Key] = feature.Value;

            var multipartContent = new MultipartContent("byteranges", boundary);

            foreach(var fragment in ViewFragments)
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
