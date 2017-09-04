using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace ImageLoading.Sample
{
    [Android.App.Activity(Label = "FFImageLoading - RecyclerView", MainLauncher = false, Theme = "@style/ImageLoading.Theme")]
    public class RecyclerActivity : AppCompatActivity
    {
        public const string POSITION = "position";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.list);

            var list = FindViewById<RecyclerView>(Resource.Id.venueList);
            var adapter = new PictureAdapter(this);
            adapter.Items = Config.Images;

            RecyclerView.LayoutManager layoutManagerDelegate = new LinearLayoutManager(this);
            list.SetLayoutManager(layoutManagerDelegate);
            list.SetAdapter(adapter);
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
}
