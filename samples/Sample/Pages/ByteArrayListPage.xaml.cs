
namespace Sample
{
    public partial class ByteArrayListPage : ContentPage
    {
		ByteArrayListPageModel viewModel;

		public ByteArrayListPage()
        {
            InitializeComponent();
			BindingContext = viewModel = new ByteArrayListPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}

		void ListView_ItemSelected(System.Object sender, Microsoft.Maui.Controls.SelectedItemChangedEventArgs e)
		{
			(BindingContext as ByteArrayListPageModel).ItemSelected();
		}
	}
}
