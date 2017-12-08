using Xamarin.Forms;

namespace Simple.TizenForms.Sample
{
    public class App : Application
    {
        public App()
        {
            // The root page of your application
            var navi = new NavigationPage();
            navi.PushAsync(new MainPage());
            MainPage = navi;
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
