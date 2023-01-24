
namespace Sample
{
    public partial class ByteArrayListPage : ContentPage
    {
        public ByteArrayListPage()
        {
            InitializeComponent();
			BindingContext = new ByteArrayListPageModel();
		}

		void ListView_ItemSelected(System.Object sender, Microsoft.Maui.Controls.SelectedItemChangedEventArgs e)
		{
			(BindingContext as ByteArrayListPageModel).ItemSelected();
		}
	}
}
