using System.Collections.Generic;
using Xamarin.Forms;
using FFImageLoading.Forms;

namespace Simple.TizenForms.Sample
{
    public class ListViewPage : ContentPage
    {
        public static List<string> Images = new List<string>
        {
            "img1.png",
            "img1.png",
            "img1.png",
            "img1.png",
            "img1.png",
            "img1.png",
            "img1.png",
            "img1.png",
            "img1.png",
            "img1.png",
            "img1.png",
            "img1.png",
            "http://i.imgur.com/H8mbyUM.jpg",
            "http://i.imgur.com/6ZUYWDE.jpg",
            "http://i.imgur.com/t9gomWN.jpg",
            "http://i.imgur.com/9f974SC.jpg",
            "http://i.imgur.com/KQ3NtX5.jpg",
            "http://i.imgur.com/ygrDkwo.jpg",
            "http://i.imgur.com/SZOzETV.jpg",
            "http://i.imgur.com/YQlie8b.jpg",
            "http://i.imgur.com/kJS4q3U.jpg",
            "http://i.imgur.com/MkH9aPa.jpg",
            "http://i.imgur.com/C3b9otw.jpg",
            "http://i.imgur.com/wpcsCMh.jpg",
            "http://i.imgur.com/fH6dqpP.jpg",
            "http://i.imgur.com/T38xuv3.jpg",
            "http://i.imgur.com/jZ4qSxo.jpg",
            "http://i.imgur.com/hb7ICRv.jpg",
            "http://i.imgur.com/Ddqmjin.jpg",
            "http://i.imgur.com/P2K8oew.jpg",
            "http://i.imgur.com/vmKxhfq.jpg",
            "http://i.imgur.com/JJxzvB3.jpg",
            "http://i.imgur.com/EAIOrXC.jpg",
            "http://i.imgur.com/sJQ1M5e.jpg",
            "http://i.imgur.com/gkVgpL1.jpg",
            "http://i.imgur.com/zmwV8Ba.jpg",
            "http://i.imgur.com/waDmCJX.jpg",
            "http://i.imgur.com/urGrRPC.jpg",
            "http://i.imgur.com/T7N6HNM.jpg",
            "http://i.imgur.com/tdrJDWq.jpg",
            "http://i.imgur.com/1QOBitB.jpg",
            "http://i.imgur.com/vRktkuz.jpg",
            "http://i.imgur.com/VXkrtKO.jpg",
        };
        public ListViewPage ()
        {
            Title = "CachedImage in ListView";
            var items = new List<ImageItem>();
            for (int i = 0; i < Images.Count; i++)
            {
                items.Add(new ImageItem
                {
                    Source = Images[i],
                    Title = Images[i]
                });
            }

            Content = new ListView {
                HasUnevenRows = false,
                RowHeight = 320,
                ItemsSource = items,
                ItemTemplate = new DataTemplate(() =>
                {
                    var cell = new ViewCell();
                    var label = new Label
                    {
                        VerticalOptions = LayoutOptions.Start,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                    };
                    var image = new CachedImage
                    {
                        HeightRequest = 300,
                        ErrorPlaceholder = "error.jpg",
                        LoadingPlaceholder = "placeholder.jpg",
                        VerticalOptions = LayoutOptions.CenterAndExpand,
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                    };
                    label.SetBinding(Label.TextProperty, new Binding("Title"));
                    image.SetBinding(CachedImage.SourceProperty, new Binding("Source"));

                    cell.View = new StackLayout
                    {
                        Children =
                        {
                            label,
                            image
                        }
                    };

                    return cell;
                })
            };
        }

        class ImageItem
        {
            public ImageSource Source { get; set; }
            public string Title { get; set; }
        }
    }
}