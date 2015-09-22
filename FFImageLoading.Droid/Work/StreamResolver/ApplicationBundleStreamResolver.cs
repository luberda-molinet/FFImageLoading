using System;
using Android.Graphics.Drawables;
using System.IO;
using FFImageLoading.Work;
using Android.Content;
using Android.Content.Res;
using System.Threading.Tasks;

namespace FFImageLoading
{
	public class ApplicationBundleStreamResolver : IStreamResolver
	{

		private Context Context {
			get {
				return global::Android.App.Application.Context.ApplicationContext;
			}
		}

		public async Task<WithLoadingResult<Stream>> GetStream(string identifier)
		{
			var resourceId = Context.Resources.GetIdentifier (identifier.ToLower (), "drawable", Context.PackageName);
			Stream stream = null;
			if (resourceId != 0)
			{
				stream = Context.Resources.OpenRawResource (resourceId);
			}
			return WithLoadingResult.Encapsulate(stream, LoadingResult.ApplicationBundle);
		}

		public void Dispose() {
		}
		
	}
}

