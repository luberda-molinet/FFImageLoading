using System;
using Android.Graphics.Drawables;
using FFImageLoading.Work;
using System.Threading.Tasks;
using Android.Content;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace FFImageLoading.Work.StreamResolver
{
	public class CompiledResourceStreamResolver : IStreamResolver
	{
		private static ConcurrentDictionary<string, int> _resourceIdentifiersCache = new ConcurrentDictionary<string, int>();

		private Context Context {
			get {
				return Android.App.Application.Context.ApplicationContext;
			}
		}

		public Task<WithLoadingResult<Stream>> GetStream(string identifier, CancellationToken token)
		{
			// Resource name is always without extension
			string resourceName = Path.GetFileNameWithoutExtension(identifier);

			int resourceId = 0;
			if (!_resourceIdentifiersCache.TryGetValue(resourceName, out resourceId))
			{
				resourceId = Context.Resources.GetIdentifier(resourceName.ToLower(), "drawable", Context.PackageName);
				_resourceIdentifiersCache.TryAdd(resourceName.ToLower(), resourceId);
			}

			Stream stream = null;
			if (resourceId != 0)
			{
				stream = Context.Resources.OpenRawResource(resourceId);
			}
			else
			{
				return Task.FromResult(WithLoadingResult.Encapsulate((Stream)null, LoadingResult.NotFound));
			}

			var result = WithLoadingResult.Encapsulate(stream, LoadingResult.CompiledResource);
			return Task.FromResult(result);
		}

		public void Dispose() {
		}
		
	}
}

