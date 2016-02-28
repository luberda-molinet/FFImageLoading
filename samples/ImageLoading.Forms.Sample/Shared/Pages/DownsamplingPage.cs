using System;
using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.PageModels;

namespace FFImageLoading.Forms.Sample.Pages
{
    public class DownsamplingPage : ContentPage, IBasePage<DownsamplingPageModel>
	{
		public DownsamplingPage()
		{
			Title = "Downsampling example";

			var cachedImage = new CachedImage() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				CacheDuration = TimeSpan.FromDays(30),
			};

            cachedImage.SetBinding<DownsamplingPageModel>(CachedImage.SourceProperty, v => v.ImagePath);
            cachedImage.SetBinding<DownsamplingPageModel>(CachedImage.DownsampleWidthProperty, v => v.DownsampleWidth);
            cachedImage.SetBinding<DownsamplingPageModel>(CachedImage.DownsampleHeightProperty, v => v.DownsampleHeight);

			var button1 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Downsample Height 200",
			};
            button1.SetBinding<DownsamplingPageModel>(Button.CommandProperty, v => v.DownsampleHeight200Command);

			var button2 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Downsample Width 200",
			};
            button1.SetBinding<DownsamplingPageModel>(Button.CommandProperty, v => v.DownsampleWidth200Command);

			var imagePath = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
			};
            imagePath.SetBinding<DownsamplingPageModel>(Label.TextProperty, v => v.ImagePath);

			Content = new ScrollView() {
				Content = new StackLayout { 
					Children = {
						cachedImage,
						imagePath,
						button1, 
						button2, 
					}
				}
			};
		}

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            this.GetPageModel()
                .FreeResources();
        }
	}
}


