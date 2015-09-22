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
			return WithLoadingResult.Encapsulate(Context.Assets.Open(identifier, Access.Streaming), LoadingResult.ApplicationBundle);
		}

		public void Dispose() {
		}
		
	}
}

