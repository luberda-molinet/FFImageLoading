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
			try
			{
				var imageInformation = new ImageInformation();
				imageInformation.SetPath(identifier);
				imageInformation.SetFilePath(null);

				var result = WithLoadingResult.Encapsulate(Context.Assets.Open(identifier, Access.Streaming), 
					LoadingResult.ApplicationBundle, imageInformation);
				return Task.FromResult(result);
			}
			catch (Java.IO.FileNotFoundException)
			{
				return Task.FromResult(WithLoadingResult.Encapsulate((Stream)null, LoadingResult.NotFound));
			}
			catch (Java.IO.IOException)
			{
				return Task.FromResult(WithLoadingResult.Encapsulate((Stream)null, LoadingResult.NotFound));
			}
			catch (FileNotFoundException)
			{
				return Task.FromResult(WithLoadingResult.Encapsulate((Stream)null, LoadingResult.NotFound));
			}
			catch (IOException)
			{
				return Task.FromResult(WithLoadingResult.Encapsulate((Stream)null, LoadingResult.NotFound));
			}
		}

		public void Dispose() {
		}
		
	}
}

