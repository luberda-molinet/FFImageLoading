using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using Android.Support.V4.View;
using FFImageLoading;
using FFImageLoading.Views;
using Android.Content.Res;
using Android.Support.V4.App;
using Android.Support.V7.App;

namespace ImageLoading.Sample
{
    [Android.App.Activity(Label = "FFImageLoading - Swipe", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/ImageLoading.Theme")]
    public class MainActivity : AppCompatActivity
    {
        public const string POSITION = "position";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            var position = Intent.GetIntExtra(POSITION, 0);

            var viewPager = FindViewById<ViewPager>(Resource.Id.pager);
            viewPager.SetClipToPadding(false);
            viewPager.PageMargin = DimensionHelper.DpToPx(12);

            viewPager.Adapter = new SwipeGalleryStateAdapter(SupportFragmentManager, Config.Images);
            viewPager.SetCurrentItem(position, false);
        }

        public static class DimensionHelper
        {
            public static int DpToPx(int dp)
            {
                return (int)(dp * Resources.System.DisplayMetrics.Density);
            }

            public static int PxToDp(int px)
            {
                return (int)(px / Resources.System.DisplayMetrics.Density);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.options, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.btnSwipe:
                    StartActivity(typeof(MainActivity));
                    return true;
                case Resource.Id.btnRecycler:
                    StartActivity(typeof(RecyclerActivity));
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }
    }

    public class SwipeGalleryStateAdapter : FragmentStatePagerAdapter
    {
        private List<string> images;

        public SwipeGalleryStateAdapter(Android.Support.V4.App.FragmentManager fm, List<string> images) : base(fm)
        {
            this.images = images;
        }

        public override int Count
        {
            get { return images.Count; }
        }
        public override float GetPageWidth(int position)
        {
            return 0.93f;
        }

        public override Fragment GetItem(int position)
        {
            ImageFragment f = new ImageFragment(position);
            return f;
        }
    }
}

