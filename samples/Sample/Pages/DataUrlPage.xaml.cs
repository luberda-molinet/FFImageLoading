
namespace Sample
{
    public partial class DataUrlPage : ContentPage
    {
        public DataUrlPage()
        {
            InitializeComponent();

			BindingContext = new DataUrlPageModel();

		}
    }
}
