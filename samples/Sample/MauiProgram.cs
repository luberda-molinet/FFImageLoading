using Microsoft.Extensions.Logging;
using FFImageLoading.Maui;

namespace Sample
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				})
				.ConfigureImageSources(srcs => srcs.UseFFImageLoading());

#if DEBUG
		builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}

	public static class Helpers
	{
		public static string GetImageUrl(int key, int width = 600, int height = 600)
		{
			return $"https://loremflickr.com/{width}/{height}/nature?random={key}";
		}

		public static string GetRandomImageUrl(int width = 600, int height = 600)
		{
			return $"https://loremflickr.com/{width}/{height}/nature?random={Guid.NewGuid()}";
		}
	}
}
