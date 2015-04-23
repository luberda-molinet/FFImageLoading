using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using FFImageLoading.Work;
using ImageLoading.Sample.Transformations;

namespace ImageLoading.Sample
{
    public class ImageFragment : Fragment
    {
        int position;

        public ImageFragment(int position = 0)
        {
            this.position = position;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
			var view = inflater.Inflate (Resource.Layout.fragment_image, container, false);
		
			var imgDisplay = view.FindViewById<ImageViewAsync>(Resource.Id.imgDisplay);
			var textView = view.FindViewById<TextView>(Resource.Id.textView);
		
            var urlToImage = Config.Images[position];

            ImageService.LoadUrl(urlToImage)
                .Retry(3, 200)
                .DownSample(300, 300)
                .Transform(new CropCircleTransformation())
                .Transform(new GrayscaleTransformation())
                .LoadingPlaceholder(Config.LoadingPlaceholderPath, ImageSource.ApplicationBundle)
                .ErrorPlaceholder(Config.ErrorPlaceholderPath, ImageSource.ApplicationBundle)
                .Into(imgDisplay);

			textView.Text = position.ToString();

			return view;
        }


    }
}