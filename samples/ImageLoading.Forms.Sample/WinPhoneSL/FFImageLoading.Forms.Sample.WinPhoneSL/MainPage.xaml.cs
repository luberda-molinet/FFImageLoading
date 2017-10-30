using FFImageLoading.Forms.WinSL;
using Xamarin.Forms.Platform.WinPhone;

namespace FFImageLoading.Forms.Sample.WinPhoneSL
{
    public partial class MainPage : FormsApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            CachedImageRenderer.Init();

            global::Xamarin.Forms.Forms.Init();

            LoadApplication(new Sample.App());
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}
