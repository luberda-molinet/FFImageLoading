using System;

namespace FFImageLoading.Work
{
    public interface IVectorDataResolver : IDataResolver
    {
        int VectorWidth { get; }

        int VectorHeight { get; }

        bool UseDipUnits { get; }
    }
}
