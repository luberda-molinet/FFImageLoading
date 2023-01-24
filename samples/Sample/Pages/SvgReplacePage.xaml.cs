
namespace Sample
{
    public partial class SvgReplacePage : ContentPage
    {
        public SvgReplacePage()
        {
            InitializeComponent();
            BindingContext = new SvgReplacePageModel();
        }
    }
}
