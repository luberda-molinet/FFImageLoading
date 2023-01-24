using System;
using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.Work;

namespace FFImageLoading.DataResolvers
{
    public class WrappedDataResolverFactory : IDataResolverFactory
    {
        readonly IDataResolverFactory _factory;

        public WrappedDataResolverFactory(IDataResolverFactory factory)
        {
            _factory = factory;
        }

        public IDataResolver GetResolver(string identifier, Work.ImageSource source, TaskParameter parameters)
        {
            return new WrappedDataResolver(_factory.GetResolver(identifier, source, parameters));
        }
    }
}
