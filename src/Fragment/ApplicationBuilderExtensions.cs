using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;

namespace Fragment
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseFragment(this IApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null) throw new ArgumentNullException(nameof(applicationBuilder));

            var assembly = typeof(ApplicationBuilderExtensions).Assembly;

            var embeddedFileProvider = new EmbeddedFileProvider(
                assembly,
                $"{assembly.GetName().Name}.content"
            );

            applicationBuilder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = embeddedFileProvider,
                RequestPath = new PathString("/fragment")
            });

            return applicationBuilder;
        }
    }
}
