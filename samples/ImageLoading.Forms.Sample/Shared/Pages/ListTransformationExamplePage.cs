using System;

using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.ViewModels;
using FFImageLoading.Forms.Sample.Models;
using System.Collections.Generic;
using FFImageLoading.Transformations;
using FFImageLoading.Work;

namespace FFImageLoading.Forms.Sample.Pages
{
	public class ListTransformationExamplePage :  PFContentPage<ListTransformationExampleViewModel>
	{
		public ListTransformationExamplePage()
		{
			Title = "List Transformations Demo";

			var listView = new ListView() {
				HorizontalOptions = LayoutOptions.FillAndExpand, 
				VerticalOptions = LayoutOptions.FillAndExpand,
				ItemTemplate = new DataTemplate(typeof(TransformationExampleCell)),
				HasUnevenRows = false,
				RowHeight = 210,
			};

			listView.SetBinding<ListTransformationExampleViewModel>(ListView.ItemsSourceProperty, v => v.Items);

            if (Device.OS == TargetPlatform.Android || Device.OS == TargetPlatform.iOS)
                listView.ItemSelected += (sender, e) => { listView.SelectedItem = null; };

			Content = listView;
		}

		class TransformationExampleCell : ViewCell
		{
			public TransformationExampleCell()
			{
				var image = new CachedImage() {
					WidthRequest = 200,
					HeightRequest = 200,
					DownsampleHeight = 200,
					DownsampleUseDipUnits = true,
					TransparencyEnabled = false,
					Aspect = Aspect.AspectFill,
					CacheDuration = TimeSpan.FromDays(30),
					RetryCount = 3,
					RetryDelay = 500,
					LoadingPlaceholder = "loading.png",
					Transformations = new List<ITransformation>() {
						// new SepiaTransformation(),
						// new ColorSpaceTransformation(FFColorMatrix.InvertColorMatrix),
						// new BlurredTransformation(10),
						new GrayscaleTransformation(),
						new RoundedTransformation(60),
						// new CornersTransformation(50, 50, 50, 50, CornerTransformType.AllRounded),
					}
				};
				image.SetBinding<ListExampleItem>(CachedImage.SourceProperty, v => v.ImageUrl);

				var fileName = new Label() {
					LineBreakMode = LineBreakMode.CharacterWrap,
					YAlign = TextAlignment.Center,
					XAlign = TextAlignment.Center,
				};
				fileName.SetBinding<ListExampleItem>(Label.TextProperty, v => v.FileName);

				var root = new AbsoluteLayout() {
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand,
					Padding = 5,
				};

				root.Children.Add(image, new Rectangle(0f, 0f, 200f, 200f));
				root.Children.Add(fileName, new Rectangle(200f, 0f, 150f, 200f));

				View = root;	
			}
		}
	}
}


