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
using FFImageLoading.Transformations;

namespace ImageLoading.Sample
{
    public class ImageFragment : Fragment
    {
        private int _position;
        private ImageView _imgDisplay;

        public ImageFragment(int position = 0)
        {
            this._position = position;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.fragment_image, container, false);

            using (var textView = view.FindViewById<TextView>(Resource.Id.textView))
            {
                textView.Text = _position.ToString();
            }

            _imgDisplay = view.FindViewById<ImageView>(Resource.Id.imgDisplay);
            var urlToImage = Config.Images[_position];

            ImageService.Instance.LoadUrl(urlToImage)
                .Retry(3, 200)
                .DownSample(300, 300)
                .Transform(new CircleTransformation())
                .Transform(new GrayscaleTransformation())
                .LoadingPlaceholder(Config.LoadingPlaceholderPath, ImageSource.ApplicationBundle)
                .ErrorPlaceholder(Config.ErrorPlaceholderPath, ImageSource.ApplicationBundle)
                .Into(_imgDisplay);

            return view;
        }

        public override void OnDestroyView()
        {
            if (_imgDisplay != null)
            {
                _imgDisplay.TryDispose();
                _imgDisplay = null;
            }

            base.OnDestroyView();
        }
    }
}
