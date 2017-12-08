using System.IO;
using Xamarin.Forms;
using FFImageLoading.Forms;

namespace Simple.TizenForms.Sample
{
    public class GetImageTestPage : ContentPage
    {
        Button _copyJPG;
        Button _copyPNG;
        CachedImage _copiedImageJpg;
        CachedImage _copiedImagePng;
        public GetImageTestPage()
        {
            Title = "GetImage Test Page";
            var img = new CachedImage
            {
                Source = "http://i.imgur.com/6ZUYWDE.jpg",
                LoadingPlaceholder = "placeholder.jpg",
            };
            _copiedImageJpg = new CachedImage
            {
                LoadingPlaceholder = "placeholder.jpg"
            };
            _copiedImagePng = new CachedImage
            {
                LoadingPlaceholder = "placeholder.jpg"
            };


            img.Finish += OnFinish;

            _copyJPG = new Button() { Text = "Copy as JPG", IsEnabled = false };
            _copyPNG = new Button() { Text = "Copy as PNG", IsEnabled = false };
            _copyJPG.Clicked += async (s, e) =>
            {
                var data = await img.GetImageAsJpgAsync();
                _copiedImageJpg.Source = ImageSource.FromStream(()=> {
                    return new MemoryStream(data);
                });
            };
            _copyPNG.Clicked += async (s, e) =>
            {
                var data = await img.GetImageAsPngAsync();
                _copiedImagePng.Source = ImageSource.FromStream(() => {
                    return new MemoryStream(data);
                });
            };

            Content = new ScrollView
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Children =
                    {
                        img,
                        _copyJPG,
                        _copyPNG,
                        _copiedImageJpg,
                        _copiedImagePng
                    }
                }
            };

        }

        void OnFinish(object sender, CachedImageEvents.FinishEventArgs e)
        {
            _copyJPG.IsEnabled = true;
            _copyPNG.IsEnabled = true;
        }
    }
}