using System;

using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.ViewModels;
using FFImageLoading.Forms.Sample.Models;
using System.Collections.Generic;
using FFImageLoading.Transformations;

namespace FFImageLoading.Forms.Sample.Pages
{
	public class ListHeavyTestPage : PFContentPage<ListHeavyTestViewModel>
	{
		public ListHeavyTestPage()
		{
			Title = "HeavyList Demo";

			var listView = new ListView() {
				HorizontalOptions = LayoutOptions.FillAndExpand, 
				VerticalOptions = LayoutOptions.FillAndExpand,
				ItemTemplate = new DataTemplate(typeof(ListExampleCell)),
				HasUnevenRows = false,
				RowHeight = 110,
			};
			listView.SetBinding<ListHeavyTestViewModel>(ListView.ItemsSourceProperty, v => v.Items);

			if (Device.OS == TargetPlatform.Android || Device.OS == TargetPlatform.iOS)
				listView.ItemSelected += (sender, e) => { listView.SelectedItem = null; };

			Content = listView;
		}

		class ListExampleCell : ViewCell
		{
			readonly CachedImage image1 = null;
			readonly CachedImage image2 = null;
			readonly CachedImage image3 = null;
			readonly CachedImage image4 = null;


			public ListExampleCell()
			{
				image1 = new CachedImage() {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = 100,
                    DownsampleToViewSize = true,
                    TransparencyEnabled = false,
                    Aspect = Aspect.AspectFill,
                    CacheDuration = TimeSpan.FromDays(30),
                    RetryCount = 3,
                    RetryDelay = 500,
                    TransformPlaceholders = false,
                    LoadingPlaceholder = "loading.png",
                    ErrorPlaceholder = "error.png",
                };

				image2 = new CachedImage() {
					HorizontalOptions = LayoutOptions.FillAndExpand,
					HeightRequest = 100,
					DownsampleToViewSize = true,
					TransparencyEnabled = false,
					Aspect = Aspect.AspectFill,
					CacheDuration = TimeSpan.FromDays(30),
					RetryCount = 3,
					RetryDelay = 500,
					TransformPlaceholders= false,
					LoadingPlaceholder = "loading.png",
					ErrorPlaceholder = "error.png",
					Transformations = new List<FFImageLoading.Work.ITransformation>() {
						new GrayscaleTransformation()
					}
				};

				image3 = new CachedImage() {
					HorizontalOptions = LayoutOptions.FillAndExpand,
					HeightRequest = 100,
					DownsampleToViewSize = true,
					TransparencyEnabled = true,
					Aspect = Aspect.AspectFill,
					CacheDuration = TimeSpan.FromDays(30),
					RetryCount = 3,
					RetryDelay = 500,
					TransformPlaceholders= true,
					LoadingPlaceholder = "loading.png",
					ErrorPlaceholder = "error.png",
					Transformations = new List<FFImageLoading.Work.ITransformation>() {
						new CornersTransformation(50, CornerTransformType.RightRounded)
					}
				};

				image4 = new CachedImage() {
					HorizontalOptions = LayoutOptions.FillAndExpand,
					HeightRequest = 100,
					DownsampleToViewSize = true,
					TransparencyEnabled = true,
					Aspect = Aspect.AspectFill,
					CacheDuration = TimeSpan.FromDays(30),
					RetryCount = 3,
					RetryDelay = 500,
					TransformPlaceholders= true,
					LoadingPlaceholder = "loading.png",
					ErrorPlaceholder = "error.png",
					Transformations = new List<FFImageLoading.Work.ITransformation>() {
						new CircleTransformation()
					}
				};

				var root = new AbsoluteLayout() {
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand,
					Padding = 5,
				};

                AbsoluteLayout.SetLayoutFlags(image1, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(image1, new Rectangle(0d, 0d, 0.25d, 1d));

                AbsoluteLayout.SetLayoutFlags(image2, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(image2, new Rectangle(0.25d / (1d - 0.25d), 0d, 0.25d, 1d));

                AbsoluteLayout.SetLayoutFlags(image3, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(image3, new Rectangle(0.50d / (1d - 0.25d), 0d, 0.25d, 1d));

                AbsoluteLayout.SetLayoutFlags(image4, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(image4, new Rectangle(0.75d / (1d - 0.25d), 0d, 0.25d, 1d));

                root.Children.Add(image1);
                root.Children.Add(image2);
                root.Children.Add(image3);
                root.Children.Add(image4);

                View = root;	
            }

            protected override void OnBindingContextChanged()
			{
				base.OnBindingContextChanged();

				var item = BindingContext as ListHeavyItem;

				if (item == null)
					return;

				image1.Source = item.Image1Url;
				image2.Source = item.Image2Url;
				image3.Source = item.Image3Url;
				image4.Source = item.Image4Url;
			}
		}
	}
}


