using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Fragment
{
    public static class HeaderDictionaryExtensions
    {
        public static void AddOrUpdateIfValue(this IHeaderDictionary headerDictionary, string key, object value)
        {
            if (value == null) return;
            headerDictionary[key] = value.ToString();
        }
    }
}
