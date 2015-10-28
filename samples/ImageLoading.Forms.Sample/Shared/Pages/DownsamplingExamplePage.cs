using System;

using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.ViewModels;

namespace FFImageLoading.Forms.Sample.Pages
{
	public class DownsamplingExamplePage : PFContentPage<DownsamplingExampleViewModel>
	{
		public DownsamplingExamplePage()
		{
			Title = "Downsampling example";

			var cachedImage = new CachedImage() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				CacheDuration = TimeSpan.FromDays(30),
			};

			cachedImage.SetBinding<DownsamplingExampleViewModel>(CachedImage.SourceProperty, v => v.ImagePath);
			cachedImage.SetBinding<DownsamplingExampleViewModel>(CachedImage.DownsampleWidthProperty, v => v.DownsampleWidth);
			cachedImage.SetBinding<DownsamplingExampleViewModel>(CachedImage.DownsampleHeightProperty, v => v.DownsampleHeight);

			var button1 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Downsample Height 200",
				Command = ViewModel.DownsampleHeight200ExampleCommand
			};

			var button2 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Downsample Width 200",
				Command = ViewModel.DownsampleWidth200ExampleCommand
			};

			var imagePath = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
			};
			imagePath.SetBinding<DownsamplingExampleViewModel>(Label.TextProperty, v => v.ImagePath);

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
	}
}


