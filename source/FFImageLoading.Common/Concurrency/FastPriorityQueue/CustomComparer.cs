using System;
using System.Collections.Generic;

namespace FFImageLoading
{
    public class QueueComparer<TPriority> : Comparer<TPriority>
    {
        Comparer<TPriority> _comparer = Comparer<TPriority>.Default;

        public override int Compare(TPriority x, TPriority y)
        {
            return _comparer.Compare(x, y) * -1;
        }
    }
}
