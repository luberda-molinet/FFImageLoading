using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using FFImageLoading.Transformations;

namespace ImageLoading.Sample
{
    [Android.App.Activity(Label = "FFImageLoading - Detail", Theme = "@style/ImageLoading.Theme")]
    public class DetailActivity : AppCompatActivity
    {
        public const string POSITION = "Position";

        ImageView backgroundImage, logoImage;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.detail_item);

            backgroundImage = FindViewById<ImageView>(Resource.Id.backgroundImage);
            logoImage = FindViewById<ImageView>(Resource.Id.logoImage);
            var txtTitle = FindViewById<TextView>(Resource.Id.txtTitle);

            var position = Intent.GetIntExtra (POSITION, 0);
            var image = Config.Images [position];

            txtTitle.Text = position.ToString ();

            ImageService.Instance.LoadUrl(image)
               .Retry(3, 200)
               .DownSample(200, 200)
                .LoadingPlaceholder(Config.LoadingPlaceholderPath, FFImageLoading.Work.ImageSource.ApplicationBundle)
                .ErrorPlaceholder(Config.ErrorPlaceholderPath, FFImageLoading.Work.ImageSource.ApplicationBundle)
               .Into(logoImage);

            ImageService.Instance.LoadUrl(image)
                .Retry(3, 200)
                .DownSample(500, 500)
                .Transform(new BlurredTransformation(10))
                .LoadingPlaceholder(Config.LoadingPlaceholderPath, FFImageLoading.Work.ImageSource.ApplicationBundle)
                .ErrorPlaceholder(Config.ErrorPlaceholderPath, FFImageLoading.Work.ImageSource.ApplicationBundle)
                .Into(backgroundImage);
        }
    }
}
