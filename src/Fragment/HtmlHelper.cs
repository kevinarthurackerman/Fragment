using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;

namespace Fragment
{
    public static class HtmlHelper
    {
        public static IHtmlContent RenderScripts() =>
            new MultiHtmlContent(RenderInitializationScripts(), RenderDynamicScriptsContainer());

        public static IHtmlContent RenderInitializationScripts()
        {
            // todo: add min file and split behavior on development
            var assembly = typeof(HtmlHelper).Assembly;
            var embeddedFileProvider = new EmbeddedFileProvider(
                assembly,
                $"{assembly.GetName().Name}.content"
            );

            var fileInfo = embeddedFileProvider.GetFileInfo("core.js");
            using var fileStream = new MemoryStream();
            fileInfo.CreateReadStream().CopyTo(fileStream);

            var fileData = fileStream.ToArray();
            var hash = MD5.Create().ComputeHash(fileData);
            
            var version = Convert.ToBase64String(hash);
            var builder = new HtmlContentBuilder();
            builder.AppendHtmlLine($"<script src='/fragment/core.js?v=${version}'></script>");

            return builder;
        }

        public static IHtmlContent RenderDynamicScriptsContainer()
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
