using Tizen.Applications;

namespace Simple.TizenForms.Sample
{
    class Program : Xamarin.Forms.Platform.Tizen.FormsApplication
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            var config = new FFImageLoading.Config.Configuration()
            {
                ExecuteCallbacksOnUIThread = true,
                VerboseLogging = true,
                VerboseLoadingCancelledLogging = true,
                VerboseMemoryCacheLogging = true,
            };
            FFImageLoading.ImageService.Instance.Initialize(config);

            LoadApplication(new App());
            MainWindow.AvailableRotations = ElmSharp.DisplayRotation.Degree_0 | ElmSharp.DisplayRotation.Degree_90;
        }

        protected override void OnLowMemory(LowMemoryEventArgs e)
        {
            base.OnLowMemory(e);
            FFImageLoading.ImageService.Instance.InvalidateMemoryCache();
        }

        static void Main(string[] args)
        {
            var app = new Program();
            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(app);
            Xamarin.Forms.Platform.Tizen.Forms.Init(app, true);
            app.Run(args);
        }
    }
}
