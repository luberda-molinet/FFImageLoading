using System;
using Android.Graphics.Drawables;
using FFImageLoading.Work;
using System.Threading.Tasks;
using Android.Content;
using System.IO;

namespace FFImageLoading
{
	public class CompiledResourceStreamResolver : IStreamResolver
	{

		private Context Context {
			get {
				return global::Android.App.Application.Context.ApplicationContext;
			}
		}

		public async Task<WithLoadingResult<Stream>> GetStream(string identifier)
		{
			int resourceId = 0;
			int? cachedResourceId = ResourceIdentifiersCache.Instance.Get(identifier);
			if (cachedResourceId.HasValue)
			{
				resourceId = cachedResourceId.Value;
			}
			else
			{
				resourceId = Context.Resources.GetIdentifier (identifier.ToLower (), "drawable", Context.PackageName);
				ResourceIdentifiersCache.Instance.Add(identifier, resourceId);
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

