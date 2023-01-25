using System;
using System.Collections.Generic;
using FFImageLoading.Svg.Maui;
using FFImageLoading.Transformations;
using FFImageLoading.Work;
using Microsoft.Maui.Layouts;

namespace Sample.Pages
{
    public class SvgListHeavyPage : ContentPage
    {
        SvgListHeavyPageModel viewModel;

        public SvgListHeavyPage()
        {
            BindingContext = viewModel = new SvgListHeavyPageModel();

            Title = "SVG HeavyList Demo";

            var listView = new ListView(ListViewCachingStrategy.RecycleElement)
            {
                ItemTemplate = new DataTemplate(typeof(SvgListHeavyCell)),
                HasUnevenRows = false,
                RowHeight = 110,
            };


			listView.SetBinding(ListView.ItemsSourceProperty, nameof(SvgListHeavyPageModel.Items));
			
            listView.ItemSelected += (sender, e) => { listView.SelectedItem = null; };

            Content = listView;
        }

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}

		class SvgListHeavyCell : ViewCell
        {
            readonly SvgCachedImage image1 = null;
            readonly SvgCachedImage image2 = null;
            readonly SvgCachedImage image3 = null;
            readonly SvgCachedImage image4 = null;


            public SvgListHeavyCell()
            {
                image1 = new SvgCachedImage()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = 100,
                    DownsampleToViewSize = true,
                    Aspect = Aspect.AspectFill,
                    TransformPlaceholders = true,
                    LoadingPlaceholder = "loading.png",
                    ErrorPlaceholder = "error.png",
                    Transformations = new List<ITransformation>() { new CircleTransformation() { BorderSize = 4, BorderHexColor = "#000000"} }
                };

                image2 = new SvgCachedImage()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = 100,
                    DownsampleToViewSize = true,
                    Aspect = Aspect.AspectFill,
                    TransformPlaceholders = false,
                    LoadingPlaceholder = "loading.png",
                    ErrorPlaceholder = "error.png",
                };

                image3 = new SvgCachedImage()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = 100,
                    DownsampleToViewSize = true,
                    Aspect = Aspect.AspectFill,
                    TransformPlaceholders = true,
                    LoadingPlaceholder = "loading.png",
                    ErrorPlaceholder = "error.png",
                };

                image4 = new SvgCachedImage()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = 100,
                    DownsampleToViewSize = true,
                    Aspect = Aspect.AspectFill,
                    TransformPlaceholders = true,
                    LoadingPlaceholder = "loading.png",
                    ErrorPlaceholder = "error.png",
                };

                var root = new AbsoluteLayout()
                {
                    Padding = 5,
                };

                AbsoluteLayout.SetLayoutFlags(image1, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(image1, new Rect(0d, 0d, 0.25d, 1d));

                AbsoluteLayout.SetLayoutFlags(image2, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(image2, new Rect(0.25d / (1d - 0.25d), 0d, 0.25d, 1d));

                AbsoluteLayout.SetLayoutFlags(image3, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(image3, new Rect(0.50d / (1d - 0.25d), 0d, 0.25d, 1d));

                AbsoluteLayout.SetLayoutFlags(image4, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(image4, new Rect(0.75d / (1d - 0.25d), 0d, 0.25d, 1d));

                root.Children.Add(image1);
                root.Children.Add(image2);
                root.Children.Add(image3);
                root.Children.Add(image4);

                View = root;
            }

            protected override void OnBindingContextChanged()
            {
                base.OnBindingContextChanged();

                var item = BindingContext as SvgListHeavyPageModel.ListHeavyItem;

                if (item == null)
                {
                    image1.Source = null;
                    image2.Source = null;
                    image3.Source = null;
                    image4.Source = null;
                }
                else
                {
                    image1.Source = item.Image1Url;
                    image2.Source = item.Image2Url;
                    image3.Source = item.Image3Url;
                    image4.Source = item.Image4Url;
                }
            }
        }
    }
}
