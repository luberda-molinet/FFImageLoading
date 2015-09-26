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

		public async Task<WithLoadingResult<Stream>> GetStream(string identifier, CancellationToken token)
		{
			int resourceId = 0;
			if (!_resourceIdentifiersCache.TryGetValue(identifier, out resourceId))
			{
				resourceId = Context.Resources.GetIdentifier(identifier.ToLower(), "drawable", Context.PackageName);
				_resourceIdentifiersCache.TryAdd(identifier.ToLower(), resourceId);
			}

			Stream stream = null;
			if (resourceId != 0)
			{
				stream = Context.Resources.OpenRawResource (resourceId);
			}
			return WithLoadingResult.Encapsulate(stream, LoadingResult.CompiledResource);
		}

		public void Dispose() {
		}
		
	}
}

