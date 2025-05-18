using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GeminiTranslateExplain
{
    public static class ListExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this List<T> list)
        {
            return CollectionsMarshal.AsSpan(list);
        }
    }
}
