using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace Fragment
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseFragment(this IApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null) throw new ArgumentNullException(nameof(applicationBuilder));

            applicationBuilder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = App.ContentProvider,
                RequestPath = new PathString("/fragment")
            });

            return applicationBuilder;
        }
    }
}
