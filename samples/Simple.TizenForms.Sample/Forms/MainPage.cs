using System.Collections.Generic;
using Xamarin.Forms;

namespace Simple.TizenForms.Sample
{
    public class MainPage : ContentPage
    {
        public MainPage()
        {
            Title = "FFImage Tizen";

            var item = new List<TextCell>()
            {
                new TextCell
                {
                    Text = "Basic Test",
                    Command = new Command(async (page) =>
                    {
                        await (page as Page).Navigation.PushAsync(new BasicTestPage());
                    })
                },
                new TextCell
                {
                    Text = "SVG Basic Test",
                    Command = new Command(async (page) =>
                    {
                        await (page as Page).Navigation.PushAsync(new SvgBasicTestPage());
                    })
                },
                new TextCell
                {
                    Text = "DataUrl Test",
                    Command = new Command(async (page) =>
                    {
                        await (page as Page).Navigation.PushAsync(new DataUrlTestPage());
                    })
                },
                new TextCell
                {
                    Text = "DownSample Test",
                    Command = new Command(async (page) =>
                    {
                        await (page as Page).Navigation.PushAsync(new DownSampeTestPage());
                    })
                },
                new TextCell
                {
                    Text = "GetImage Test",
                    Command = new Command(async (page) =>
                    {
                        await (page as Page).Navigation.PushAsync(new GetImageTestPage());
                    })
                },
                new TextCell
                {
                    Text = "CachedImage in ListView",
                    Command = new Command(async (page) =>
                    {
                        await (page as Page).Navigation.PushAsync(new ListViewPage());
                    })
                },
                new TextCell
                {
                    Text = "CachedImage vs Image",
                    Command = new Command(async (page) =>
                    {
                        await (page as Page).Navigation.PushAsync(new ComparePage());
                    })
                },
                new TextCell
                {
                    Text = "Clear All cache",
                    Command = new Command(async (page) =>
                    {
                        await FFImageLoading.ImageService.Instance.InvalidateCacheAsync(FFImageLoading.Cache.CacheType.All);
                        (page as Page).DisplayAlert("Cache Clear", "Cache clear done", "Ok");
                    })
                },

            };


            var listview = new ListView
            {
                ItemsSource = item,
                ItemTemplate = new DataTemplate(() =>
                {
                    var cell = new TextCell();
                    cell.SetBinding(TextCell.TextProperty, new Binding("Text"));
                    return cell;
                })
            };

            listview.ItemTapped += OnItemSelected;
            Content = listview;
        }

        void OnItemSelected(object sender, ItemTappedEventArgs e)
        {
            (e.Item as TextCell).Command.Execute(this);
        }
    }
}
