using System.IO;
using System.Text;

namespace Fragment
{
    internal static class StreamExtensions
    {
        private static readonly byte[] _byteOrderMarkUtf8 = new UTF8Encoding(true).GetPreamble();

        internal static void CopyToWithoutByteOrderMark(this Stream stream, Stream copyTo)
        {
            if (stream.Length < _byteOrderMarkUtf8.Length)
            {
                stream.CopyTo(copyTo);
                return;
            }

            var firstBytes = new byte[_byteOrderMarkUtf8.Length];
            stream.Read(firstBytes);

            for (var i = 0; i < _byteOrderMarkUtf8.Length; i++)
                if (_byteOrderMarkUtf8[i] != firstBytes[i])
                {
                    copyTo.Write(firstBytes);
                    stream.CopyTo(copyTo);
                    return;
                }

            stream.CopyTo(copyTo);
        }
    }
}
