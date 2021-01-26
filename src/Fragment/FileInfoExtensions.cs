using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Fragment
{
    internal static class FileInfoExtensions
    {
        internal static string GenerateHash(this IFileInfo fileInfo)
        {
            using var fileStream = new MemoryStream();
            fileInfo.CreateReadStream().CopyTo(fileStream);

            var fileData = fileStream.ToArray();
            var hash = MD5.Create().ComputeHash(fileData);

            return Convert.ToBase64String(hash);
        }
    }
}
