using System;
using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.ViewModels;

namespace FFImageLoading.Forms.Sample
{
	public class App : Application
	{
		public App()
		{
            MainPage = new XamarinFormsPageFactory().Init<HomeViewModel, PFNavigationPage>();
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

