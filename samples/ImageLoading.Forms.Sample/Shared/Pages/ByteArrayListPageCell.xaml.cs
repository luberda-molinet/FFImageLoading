using Xamarin.Forms;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
    public partial class ByteArrayListPageCell : ViewCell
    {
        public ByteArrayListPageCell()
        {
            InitializeComponent();

            Image.CacheKeyFactory = new CustomCacheKeyFactory();
        }

        public class CustomCacheKeyFactory : ICacheKeyFactory
        {
            public string GetKey(ImageSource imageSource, object bindingContext)
            {
                var keySource = imageSource as ByteArrayListPageModel.CustomStreamImageSource;

                if (keySource == null)
                    return null;

                return keySource.Key;
            }
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            var item = BindingContext as ByteArrayListPageModel.ListItem;
            if (item == null)
                return;

            Image.Source = item.ImageSource;
            Label.Text = item.FileName;
        }
    }
}
