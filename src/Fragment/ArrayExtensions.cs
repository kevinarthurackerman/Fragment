using System;
using System.Text;

namespace Fragment
{
    internal static class ArrayExtensions
    {
        private static readonly byte[] _byteOrderMarkUtf8 = new UTF8Encoding(true).GetPreamble();

        internal static byte[] TrimByteOrderMark(this byte[] array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            if (array.Length < _byteOrderMarkUtf8.Length) return array;

            for (var i = 0; i < _byteOrderMarkUtf8.Length; i++)
                if (array[i] != _byteOrderMarkUtf8[i]) return array;

            return array[_byteOrderMarkUtf8.Length..];
        }
    }
}
