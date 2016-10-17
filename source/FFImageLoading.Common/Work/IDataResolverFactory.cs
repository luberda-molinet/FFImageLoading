using System;
using FFImageLoading.Work;
using FFImageLoading.Config;

namespace FFImageLoading
{
    public interface IDataResolverFactory
    {
        IDataResolver GetResolver(string identifier, ImageSource source, TaskParameter parameters);
    }
}
