using System;
using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.PageModels;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FFImageLoading.Forms.Sample.Pages
{
    public class StreamPage : ContentPage, IBasePage<StreamPageModel>
    {
        public StreamPage()
        {
            Title = "Stream with custom key test";

            var cachedImage = new CachedImage() {
                HeightRequest = 300,
                WidthRequest = 300,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };

            var button = new Button() {
                Text = "Load again, but as stream",

                Command = new Command(async() => {

                    var bytes = await cachedImage.GetImageAsJpgAsync();
                    cachedImage.Source = null;

                    var streamSource = new StreamImageSource() {
                        Stream = new Func<CancellationToken, Task<Stream>>((arg) => Task.FromResult<Stream>(new MemoryStream(bytes)))
                    };

                    cachedImage.CacheKeyFactory = new CustomCacheKeyFactory();
                    cachedImage.Source = streamSource;

                })

            };

            Content =  new StackLayout() {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = {
                    cachedImage,
                    button
                }
            };

            // First we load an image from http source
            cachedImage.Source = "http://loremflickr.com/600/600/nature?filename=stream.jpg";
        }

        class CustomCacheKeyFactory : ICacheKeyFactory
        {
            public string GetKey(ImageSource imageSource, object bindingContext)
            {
                return "StreamTestCustomKey";
            }
        }
    }
}


