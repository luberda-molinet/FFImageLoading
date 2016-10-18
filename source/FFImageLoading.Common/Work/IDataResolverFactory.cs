using System;
using FFImageLoading.Config;

namespace FFImageLoading.Work
{
    public interface IDataResolverFactory
    {
        IDataResolver GetResolver(string identifier, ImageSource source, TaskParameter parameters, Configuration configuration);
    }
}
