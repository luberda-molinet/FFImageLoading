using System;
namespace FFImageLoading.Forms.Sample
{
	public static class Helpers
	{
		public static string GetRandomImageUrl(int width = 600, int height = 600)
		{
			return string.Format("http://loremflickr.com/{1}/{2}/nature?filename={0}.jpg",
				Guid.NewGuid().ToString("N"), width, height);
		}
	}
}
