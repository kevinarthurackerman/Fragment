using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;

namespace Fragment
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlContent RenderFragmentScripts(this IHtmlHelper htmlHelper) =>
            new MultiHtmlContent(RenderFragmentInitializationScripts(htmlHelper), RenderFragmentDynamicScriptsContainer(htmlHelper));

        public static IHtmlContent RenderFragmentInitializationScripts(this IHtmlHelper htmlHelper)
        {
            // todo: add min file and split behavior on development

            var fileInfo = App.ContentProvider.GetFileInfo("core.js");
            var version = fileInfo.GenerateHash();

            var builder = new HtmlContentBuilder();
            builder.AppendHtmlLine($"<script src='/fragment/core.js?v=${version}'></script>");

            return builder;
        }

        public static IHtmlContent RenderFragmentDynamicScriptsContainer(this IHtmlHelper htmlHelper)
        {
            var builder = new HtmlContentBuilder();
            builder.AppendHtmlLine($"<div id='fragment-dynamic-scripts'></div>");

            return builder;
        }

        private class MultiHtmlContent : IHtmlContent
        {
            private readonly IEnumerable<IHtmlContent> htmlContents;

            public MultiHtmlContent(params IHtmlContent[] htmlContents)
            {
                this.htmlContents = htmlContents;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                foreach (var htmlContent in htmlContents)
                    htmlContent.WriteTo(writer, encoder);
            }
        }
    }
}
