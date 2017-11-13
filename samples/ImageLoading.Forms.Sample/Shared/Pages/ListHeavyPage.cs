using System;
using System.Collections.Generic;
using FFImageLoading.Transformations;
using Xamarin.Forms;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
    public class ListHeavyPage : ContentPage, IBasePage<ListHeavyPageModel>
    {
        public ListHeavyPage()
        {
            Title = "HeavyList Demo";

            var listView = new ListView(ListViewCachingStrategy.RecycleElement)
            {
                ItemTemplate = new DataTemplate(typeof(ListHeavyCell)),
                HasUnevenRows = false,
                RowHeight = 110,
            };
            listView.SetBinding<ListHeavyPageModel>(ListView.ItemsSourceProperty, v => v.Items);

            listView.ItemSelected += (sender, e) => { listView.SelectedItem = null; };

            Content = listView;
        }

        class ListHeavyCell : ViewCell
        {
            readonly CachedImage image1 = null;
            readonly CachedImage image2 = null;
            readonly CachedImage image3 = null;
            readonly CachedImage image4 = null;


            public ListHeavyCell()
            {
                image1 = new CachedImage()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = 100,
                    DownsampleToViewSize = true,
                    Aspect = Aspect.AspectFill,
                    TransformPlaceholders = false,
                    LoadingPlaceholder = "loading.png",
                    ErrorPlaceholder = "error.png",
                    LoadingDelay = 50,
                };

                image2 = new CachedImage()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = 100,
                    DownsampleToViewSize = true,
                    Aspect = Aspect.AspectFill,
                    TransformPlaceholders = false,
                    LoadingPlaceholder = "loading.png",
                    ErrorPlaceholder = "error.png",
                    LoadingDelay = 50,
                };

                image3 = new CachedImage()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = 100,
                    DownsampleToViewSize = true,
                    Aspect = Aspect.AspectFill,
                    TransformPlaceholders = true,
                    LoadingPlaceholder = "loading.png",
                    ErrorPlaceholder = "error.png",
                    Transformations = new List<Work.ITransformation>() {
                        new CornersTransformation(50, CornerTransformType.RightRounded)
                    },
                    LoadingDelay = 50,
                };

                image4 = new CachedImage()
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    HeightRequest = 100,
                    DownsampleToViewSize = true,
                    Aspect = Aspect.AspectFill,
                    TransformPlaceholders = true,
                    LoadingPlaceholder = "loading.png",
                    ErrorPlaceholder = "error.png",
                    Transformations = new List<Work.ITransformation>() {
                        new GrayscaleTransformation()
                    },
                    LoadingDelay = 50,
                };

                var root = new AbsoluteLayout()
                {
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

                var item = BindingContext as ListHeavyPageModel.ListHeavyItem;

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

