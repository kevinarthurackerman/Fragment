using Microsoft.AspNetCore.Hosting;
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

            response.ContentType = "text/html";

            if (Selector != null) headers.Add("X-Fragment-Selector", Selector);

            if (ContentPosition != null)
                headers.Add("X-Fragment-ContentPosition", Enum.GetName(typeof(ContentPositions), ContentPosition.Value));

            if (Delay != null) headers.Add("X-Fragment-Delay", Delay.Value.TotalMilliseconds.ToString());

            if (ContentPosition.HasValue
                && _contentPositionsWithoutBody.Contains(ContentPosition.Value))
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

    public abstract class AbstractContentFragment : IActionResult
    {
        protected abstract string ContentType { get; }
        public Stream Content { get; set; }
        public string RawContent { get; set; }
        public string FilePath { get; set; }
        public string Selector { get; set; }
        public ContentPositions? ContentPosition { get; set; }
        public TimeSpan? Delay { get; set; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var contentSources = new object[] { Content, RawContent, FilePath }
                .Where(x => x != null)
                .Count();

            if (contentSources > 1) throw new ArgumentException("More than one content source was specified");

            var response = context.HttpContext.Response;
            var headers = response.Headers;

            if (ContentType == null) throw new ArgumentNullException(nameof(ContentType));

            response.ContentType = ContentType;

            if (Selector != null) headers.Add("X-Fragment-Selector", Selector);

            if (ContentPosition != null)
                headers.Add("X-Fragment-ContentPosition", Enum.GetName(typeof(ContentPositions), ContentPosition.Value));

            if (Delay != null) headers.Add("X-Fragment-Delay", Delay.Value.TotalMilliseconds.ToString());

            if (Content != null)
            {
                Content.CopyToWithoutByteOrderMark(response.Body);
                Content.Dispose();
            }
            else if (RawContent != null)
            {
                await response.Body.WriteAsync(Encoding.UTF8.GetBytes(RawContent).TrimByteOrderMark());
            }
            else if (FilePath != null)
            {
                var services = context.HttpContext.RequestServices;
                var fileProvider = services.GetService<IFileProvider>()
                    ?? services.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
                
                using var fileStream = fileProvider
                    .GetFileInfo(FilePath)
                    .CreateReadStream();

                fileStream.CopyToWithoutByteOrderMark(response.Body);
            }
        }
    }

    public abstract class _ContentFragment : AbstractContentFragment
    {
        protected sealed override string ContentType => ContentTypeGetter;
        protected abstract string ContentTypeGetter { get; }
    }

    public class ContentFragment : _ContentFragment
    { 
        private string _contentType;
        public new virtual string ContentType
        {
            get => _contentType;
            set => _contentType = value;
        }

        protected sealed override string ContentTypeGetter => ContentType;
    }

    public class HtmlFragment : AbstractContentFragment
    {
        protected override string ContentType => "text/html";
    }

    public class JavascriptFragment : AbstractContentFragment
    {
        protected override string ContentType => "text/javascript";
    }
}
