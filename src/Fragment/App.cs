using Microsoft.Extensions.FileProviders;

namespace Fragment
{
    internal static class App
    {
        internal static IFileProvider ContentProvider { get; }

        static App()
        {
            var assembly = typeof(App).Assembly;

            ContentProvider = new EmbeddedFileProvider(
                assembly,
                $"{assembly.GetName().Name}.content"
            );
        }
    }
}
