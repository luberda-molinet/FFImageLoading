using System;
using FFImageLoading.Cache;
using FFImageLoading.Config;

namespace FFImageLoading.Work
{
    public interface IDataResolverFactory
    {
        IDataResolver GetResolver(string identifier, ImageSource source, TaskParameter parameters);
    }
}
