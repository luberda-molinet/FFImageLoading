namespace Sample
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

			var m = new MenuPage();

			MainPage = new NavigationPage(m);
		}
	}
}