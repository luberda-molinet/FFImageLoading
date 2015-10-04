using System;
using Android.Graphics.Drawables;
using System.IO;
using FFImageLoading.Work;
using Android.Content;
using Android.Content.Res;
using System.Threading.Tasks;
using System.Threading;

namespace FFImageLoading.Work.StreamResolver
{
	public class ApplicationBundleStreamResolver : IStreamResolver
	{

		private Context Context {
			get {
				return Android.App.Application.Context.ApplicationContext;
			}
		}

		public Task<WithLoadingResult<Stream>> GetStream(string identifier, CancellationToken token)
		{
			var result = WithLoadingResult.Encapsulate(Context.Assets.Open(identifier, Access.Streaming), LoadingResult.ApplicationBundle);
			return Task.FromResult(result);
		}

		public void Dispose() {
		}
		
	}
}

