using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamvvm;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace FFImageLoading.Forms.Sample
{
    public class App : Application
    {
        public App()
        {
            App.Current.Resources = new ResourceDictionary()
            {
                { "CustomCacheKeyFactory", new CustomCacheKeyFactory() }
            };

            CachedImage.FixedOnMeasureBehavior = true;
            CachedImage.FixedAndroidMotionEventHandler = true;

            // Xamvvm init
            var factory = new XamvvmFormsFactory(this);
            factory.RegisterNavigationPage<MenuNavigationPageModel>(() => this.GetPageFromCache<MenuPageModel>());
            XamvvmCore.SetCurrentFactory(factory);
            MainPage = this.GetPageFromCache<MenuNavigationPageModel>() as Page;

            //ImageService.Instance.LoadCompiledResource("loading.png").Preload();
            //ImageService.Instance.LoadUrl("http://loremflickr.com/600/600/nature?filename=simple.jpg").DownloadOnly();
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

