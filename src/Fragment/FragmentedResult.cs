using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fragment
{
    public class FragmentedResult : IActionResult
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { IgnoreNullValues = true };

        private static readonly HashSet<ContentPositions> _contentPositionsWithoutBody =
            new HashSet<ContentPositions> { ContentPositions.RemoveElement, ContentPositions.RemoveContent };

        private readonly Controller _controller;
        private readonly bool _navigateToPage;
        private readonly IEnumerable<IFragment> _fragments;

        public FragmentedResult(params IFragment[] viewFragments) : this(null, viewFragments) { }

        public FragmentedResult(Controller controller, params IFragment[] viewFragments) : this(controller, false, viewFragments) { }

        public FragmentedResult(Controller controller, bool navigateToPage, params IFragment[] fragments)
        {
            _controller = controller;
            _navigateToPage = navigateToPage;
            _fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            // todo: detect when request is not ajax and return a single content for page navigation instead
            if (!context.HttpContext.Request.Headers.Any(x => x.Key == "X-Requested-With" && x.Value == "XMLHttpRequest"))
                throw new InvalidOperationException($"{nameof(FragmentedResult)} responses can only be returned for requests made with XMLHttpRequest");

            if (_navigateToPage)
            {
                var setUrl = string.Concat(
                        _controller.Request.Scheme,
                        "://",
                        _controller.Request.Host.ToUriComponent(),
                        _controller.Request.PathBase.ToUriComponent(),
                        _controller.Request.Path.ToUriComponent(),
                        _controller.Request.QueryString.ToUriComponent());

                context.HttpContext.Response.Headers.Add("X-Fragment-Url", setUrl);
            }

            var body = context.HttpContext.Response.Body;

            var defaultControllerFactory = new Lazy<Controller>(() => (Controller)context.HttpContext.RequestServices
                .GetRequiredService<IControllerFactory>()
                .CreateController(new ControllerContext(context)), false);

            var multipartResult = new MultipartResult();
            var viewEngineFactory = new Lazy<ICompositeViewEngine>(() => 
            context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>(), false);
            var webHostingEnvironmentFactory = new Lazy<IWebHostEnvironment>(() => 
                context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>(), false);

            foreach (var fragment in _fragments)
            {
                var headers = new HttpHeadersCollection();

                if (fragment.ContentType == "text/html")
                {
                    if (fragment.Selector != null)
                        headers.Add("X-Fragment-Selector", fragment.Selector);

                    if (fragment.ContentPosition != null)
                        headers.Add("X-Fragment-ContentPosition", Enum.GetName(typeof(ContentPositions), fragment.ContentPosition));

                    if (fragment.Delay != null)
                        headers.Add("X-Fragment-Delay", fragment.Delay.ToString());
                }

                if (fragment.ContentPosition.HasValue
                    && _contentPositionsWithoutBody.Contains(fragment.ContentPosition.Value))
                {
                    multipartResult.Add(new MultipartContent(fragment.ContentType, new MemoryStream(), headers));
                    continue;
                }

                if (fragment.Content != null)
                {
                    multipartResult.Add(new MultipartContent(fragment.ContentType, fragment.Content, headers));
                    continue;
                }

                if (fragment.FilePath != null)
                {
                    var content = webHostingEnvironmentFactory.Value
                        .WebRootFileProvider.GetFileInfo(fragment.FilePath).CreateReadStream();
                    
                    multipartResult.Add(new MultipartContent(fragment.ContentType, content, headers));
                    continue;
                }

                var controller = fragment.Controller ?? _controller ?? defaultControllerFactory.Value;
                var viewStream = await CreateViewStream(controller, viewEngineFactory.Value, fragment.ViewName, fragment.Model, _navigateToPage);
                
                multipartResult.Add(new MultipartContent(fragment.ContentType, viewStream, headers));
            }

            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.HttpContext.Response.Body = body;
            await multipartResult.ExecuteResultAsync(context);

            foreach (var multipartContent in multipartResult)
                multipartContent.Stream.Dispose();
        }

        private static async Task<Stream> CreateViewStream(Controller controller, ICompositeViewEngine viewEngine, string viewName, object model, bool isMainPage)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            
            controller.ViewData.Model = model;
            var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, isMainPage);

            if (viewResult.Success == false) throw new InvalidOperationException($"A view with the name {viewName} could not be found");

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);

            var viewContext = new ViewContext(
                controller.ControllerContext,
                viewResult.View,
                controller.ViewData,
                controller.TempData,
                writer,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);

            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            controller.ViewData.Model = null;

            return stream;
        }
    }
}
