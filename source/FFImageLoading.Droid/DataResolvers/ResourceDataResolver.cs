﻿using System;
using FFImageLoading.Work;
using System.Threading.Tasks;
using Android.Content;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace FFImageLoading.DataResolvers
{
    public class ResourceDataResolver : IDataResolver
    {
        static ConcurrentDictionary<string, int> _resourceIdentifiersCache = new ConcurrentDictionary<string, int>();

        public virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            // Resource name is always without extension
            string resourceName = Path.GetFileNameWithoutExtension(identifier);

            int resourceId = 0;
            if (!_resourceIdentifiersCache.TryGetValue(resourceName, out resourceId))
            {
                token.ThrowIfCancellationRequested();
                resourceId = Context.Resources.GetIdentifier(resourceName.ToLowerInvariant(), "drawable", Context.PackageName);
                _resourceIdentifiersCache.TryAdd(resourceName.ToLowerInvariant(), resourceId);
            }

            if (resourceId == 0)
                throw new FileNotFoundException(identifier);

            token.ThrowIfCancellationRequested();
            Stream stream  = Context.Resources.OpenRawResource(resourceId);

            if (stream == null)
                throw new FileNotFoundException(identifier);

            var imageInformation = new ImageInformation
            {
                AndroidResourceId = resourceId
            };
            imageInformation.SetPath(identifier);
            imageInformation.SetFilePath(identifier);

            return Task.FromResult(new DataResolverResult(
                stream, LoadingResult.CompiledResource, imageInformation));
        }

        protected Context Context { get { return new ContextWrapper(Android.App.Application.Context); } }
    }
}

