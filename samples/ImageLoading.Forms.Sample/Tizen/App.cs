namespace FFImageLoading.Forms.Sample.Tizen
{
    class Program : Xamarin.Forms.Platform.Tizen.FormsApplication
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            LoadApplication(new App());
        }

        static void Main(string[] args)
        {
            var app = new Program();
            Xamarin.Forms.Platform.Tizen.Forms.Init(app);
            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(app);
            app.Run(args);
        }
    }
}