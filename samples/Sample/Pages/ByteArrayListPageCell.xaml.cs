using FFImageLoading.Maui;

namespace Sample
{
    public partial class ByteArrayListPageCell : ViewCell
    {
        CustomCacheKeyFactory viewModel;

		public ByteArrayListPageCell()
        {
            InitializeComponent();

            Image.CacheKeyFactory = viewModel = new CustomCacheKeyFactory();
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
