using System;
using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.PageModels;

namespace FFImageLoading.Forms.Sample
{
	public class App : Application
	{
		public App()
		{
            MainPage = new XamarinFormsPageFactory().Init<HomePageModel, PFNavigationPage>();

			ImageService.Instance.LoadCompiledResource("loading.png").Preload();
			ImageService.Instance.LoadUrl("http://loremflickr.com/600/600/nature?filename=simple.jpg").DownloadOnly();
        }

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}

