using System.Collections.Generic;

namespace FFImageLoading.Concurrency
{
    internal static class ArrayHelper
    {
        public static KeyValuePair<TKey, TValue>[] Empty<TKey, TValue>()
        {
            return new KeyValuePair<TKey, TValue>[0];
        }
    }
}

