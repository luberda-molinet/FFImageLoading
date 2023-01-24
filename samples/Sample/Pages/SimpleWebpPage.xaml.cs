namespace Sample
{
    public partial class SimpleWebpPage : ContentPage
    {
        public SimpleWebpPage()
        {
            InitializeComponent();

            BindingContext = new SimpleWebpPageModel();
        }
    }
}
