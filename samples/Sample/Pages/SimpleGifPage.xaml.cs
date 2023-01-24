
namespace Sample
{
    public partial class SimpleGifPage : ContentPage
    {
        public SimpleGifPage()
        {
            InitializeComponent();
            BindingContext = new SimpleGifPageModel();
        }
    }
}
