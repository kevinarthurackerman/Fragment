using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Fragment
{
    public class PartialFragment : IActionResult
    {
        private static readonly HashSet<ContentPositions> _contentPositionsWithoutBody =
            new HashSet<ContentPositions> { ContentPositions.RemoveElement, ContentPositions.RemoveContent };

        private static readonly Encoding _encoding = new UTF8Encoding(false);

        public string ViewName { get; set; }
        public object Model { get; set; }
        public string Selector { get; set; }
        public ContentPositions? ContentPosition { get; set; }
        public TimeSpan? Delay { get; set; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            var headers = response.Headers;

            headers.AddOrUpdateIfValue(HeaderNames.ContentType, "text/html");
            headers.AddOrUpdateIfValue(FragmentHeaderNames.XFragmentSelector, Selector);
            headers.AddOrUpdateIfValue(FragmentHeaderNames.XFragmentContentPosition, ContentPosition);
            headers.AddOrUpdateIfValue(FragmentHeaderNames.XFragmentDelay, Delay?.TotalMilliseconds);

            if (ContentPosition.HasValue && _contentPositionsWithoutBody.Contains(ContentPosition.Value))
                return;

            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var viewName = ViewName ?? controllerActionDescriptor?.ActionName;
            
            if (viewName == null)
                throw new InvalidOperationException($"No {nameof(ViewName)} was provided and a view name could not be determined since the context {nameof(ActionDescriptor)} is not a {nameof(ControllerActionDescriptor)}");

            var htmlHelper = context.HttpContext.RequestServices.GetRequiredService<IHtmlHelper>();
            var compositeViewEngine = context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
            var modelMetadataProvider = context.HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
            var tempDataProvider = context.HttpContext.RequestServices.GetRequiredService<ITempDataProvider>();
            var htmlHelperOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<HtmlHelperOptions>>();
            var htmlEncoder = context.HttpContext.RequestServices.GetRequiredService<HtmlEncoder>();
            
            var view = compositeViewEngine.FindView(context, viewName, false);
            var viewData = new ViewDataDictionary(modelMetadataProvider, context.ModelState);
            var tempData = new TempDataDictionary(context.HttpContext, tempDataProvider);

            view.EnsureSuccessful(Array.Empty<string>());
            
            var writer = new StreamWriter(response.Body);
            var viewContext = new ViewContext(context, view.View, viewData, tempData, writer, htmlHelperOptions.Value);
            ((IViewContextAware)htmlHelper).Contextualize(viewContext);
            htmlHelper.ViewData.Model = Model;

            await htmlHelper.RenderPartialAsync(ViewName, Model, htmlHelper.ViewData);
            writer.Flush();
        }
    }

    public class ContentFragment : IActionResult
    {
        public string ContentType { get; set; }
        public Stream Content { get; set; }
        public string RawContent { get; set; }
        public string FilePath { get; set; }
        public string Selector { get; set; }
        public ContentPositions? ContentPosition { get; set; }
        public TimeSpan? Delay { get; set; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var headers = context.HttpContext.Response.Headers;

            headers.AddOrUpdateIfValue(HeaderNames.ContentType, ContentType);
            headers.AddOrUpdateIfValue(FragmentHeaderNames.XFragmentSelector, Selector);
            headers.AddOrUpdateIfValue(FragmentHeaderNames.XFragmentContentPosition, ContentPosition);
            headers.AddOrUpdateIfValue(FragmentHeaderNames.XFragmentDelay, Delay?.TotalMilliseconds);

            await ContentHelper.WriteContent(context.HttpContext, Content, RawContent, FilePath);
        }
    }

    public class HtmlFragment : IActionResult
    {
        public string ContentType => "text/html";
        public Stream Content { get; set; }
        public string RawContent { get; set; }
        public string FilePath { get; set; }
        public string Selector { get; set; }
        public ContentPositions? ContentPosition { get; set; }
        public TimeSpan? Delay { get; set; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var headers = context.HttpContext.Response.Headers;

            headers.AddOrUpdateIfValue(HeaderNames.ContentType, ContentType);
            headers.AddOrUpdateIfValue(FragmentHeaderNames.XFragmentSelector, Selector);
            headers.AddOrUpdateIfValue(FragmentHeaderNames.XFragmentContentPosition, ContentPosition);
            headers.AddOrUpdateIfValue(FragmentHeaderNames.XFragmentDelay, Delay?.TotalMilliseconds);

            await ContentHelper.WriteContent(context.HttpContext, Content, RawContent, FilePath);
        }
    }

    public class JavascriptFragment : IActionResult
    {
        public string ContentType => "text/javascript";
        public Stream Content { get; set; }
        public string RawContent { get; set; }
        public string FilePath { get; set; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var headers = context.HttpContext.Response.Headers;

            headers.AddOrUpdateIfValue(HeaderNames.ContentType, ContentType);

            await ContentHelper.WriteContent(context.HttpContext, Content, RawContent, FilePath);
        }
    }

    internal static class ContentHelper
    {
        internal static async Task WriteContent(HttpContext httpContext, Stream content, string rawContent, string filePath)
        {
            var contentSources = new object[] { content, rawContent, filePath }
                .Where(x => x != null)
                .Count();

            if (contentSources > 1) throw new ArgumentException("More than one content source was specified");

            var body = httpContext.Response.Body;

            if (content != null)
            {
                content.CopyToWithoutByteOrderMark(body);
                content.Dispose();
                return;
            }

            if (rawContent != null)
            {
                await body.WriteAsync(Encoding.UTF8.GetBytes(rawContent).TrimByteOrderMark());
                return;
            }

            if (filePath != null)
            {
                var services = httpContext.RequestServices;
                var fileProvider = services.GetService<IFileProvider>()
                    ?? services.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;

                using var fileStream = fileProvider
                    .GetFileInfo(filePath)
                    .CreateReadStream();

                fileStream.CopyToWithoutByteOrderMark(body);
            }
        }
    }
}
