using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace QuickExplain.Models.Extensions
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
