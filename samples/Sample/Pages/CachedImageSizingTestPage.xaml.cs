
namespace Sample
{
    public partial class CachedImageSizingTestPage : ContentPage
    {
		CachedImageSizingTestPageModel viewModel;

		public CachedImageSizingTestPage()
        {
            InitializeComponent();
			BindingContext = viewModel = new CachedImageSizingTestPageModel();

		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			
		}
	}
}
