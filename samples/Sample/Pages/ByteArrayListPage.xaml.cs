
namespace Sample
{
    public partial class ByteArrayListPage : ContentPage
    {
        public ByteArrayListPage()
        {
            InitializeComponent();
			BindingContext = new ByteArrayListPageModel();
		}
    }
}
