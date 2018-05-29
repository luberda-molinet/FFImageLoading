using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Windows.Input;
using Xamvvm;
using System.Threading.Tasks;
using FFImageLoading.Transformations;

namespace FFImageLoading.Forms.Sample
{
    public partial class BasicPage : ContentPage, IBasePage<BasicPageModel>
    {
        public BasicPage()
        {
            InitializeComponent();

            Test();
        }

        public async Task Test()
        {
            ImageService.Instance.InvalidateMemoryCache();

            var stream = ImageService.Instance.LoadUrl("http://loremflickr.com/600/600/nature?filename=simple.jpg").AsJPGStreamAsync(100);

            var task = ImageService.Instance.LoadStream((cancelToken) => stream);

            var taskWithTransforms = task
                    .WithCache(FFImageLoading.Cache.CacheType.Disk)
                .Transform(new RotateTransformation(30));


            var imageSource = new StreamImageSource();
            imageSource.Stream = (c) => taskWithTransforms.AsJPGStreamAsync(100);

            testImg.Source = imageSource;
        }
    }
}
