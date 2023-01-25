namespace Sample
{
    public partial class SimpleWebpPage : ContentPage
    {
		SimpleWebpPageModel viewModel;

		public SimpleWebpPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new SimpleWebpPageModel();
        }

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
