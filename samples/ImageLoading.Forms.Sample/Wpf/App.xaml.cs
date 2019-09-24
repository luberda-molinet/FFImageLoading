using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Wpf
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
        protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			FFImageLoading.Forms.Platform.CachedImageRenderer.Init();

			var config = new FFImageLoading.Config.Configuration()
			{
				VerboseLogging = true,
				VerbosePerformanceLogging = true,
				VerboseMemoryCacheLogging = true,
				VerboseLoadingCancelledLogging = true,
			};
			FFImageLoading.ImageService.Instance.Initialize(config);
			List<Assembly> assembliesToInclude = new List<Assembly>();
			assembliesToInclude.Add(typeof(FFImageLoading.Forms.Platform.CachedImageRenderer).GetTypeInfo().Assembly);
			Xamarin.Forms.Forms.Init(assembliesToInclude);
        }
	}
}
