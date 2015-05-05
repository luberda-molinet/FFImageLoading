using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;

namespace ImageLoading.Sample
{
    public class PictureAdapter : RecyclerView.Adapter
    {
        Activity context;

        public List<string> Items = new List<string>();

        public PictureAdapter(Activity context)
        {
            this.context = context;
        }

        // Return the size of your dataset (invoked by the layout manager)
        public override int ItemCount
        {
            get { return Items != null ? Items.Count : 0; }
        }

        // Create new views (invoked by the layout manager)
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup viewGroup, int position)
        {
            View view = LayoutInflater.From(viewGroup.Context).Inflate(Resource.Layout.list_item, viewGroup, false);
            return new ViewHolder(view);
        }

        // Replace the contents of a view (invoked by the layout manager)
        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var item = Items[position];

            var vh = viewHolder as ViewHolder;

            vh.Title.Text = position.ToString();

            ImageService.LoadUrl(item)
               .Retry(3, 200)
               .DownSample(100, 100)
               .LoadingPlaceholder(Config.LoadingPlaceholderPath, FFImageLoading.Work.ImageSource.ApplicationBundle)
               .ErrorPlaceholder(Config.ErrorPlaceholderPath, FFImageLoading.Work.ImageSource.ApplicationBundle)
               .Into(vh.Image);
        }

        public class ViewHolder : RecyclerView.ViewHolder
        {
            public ImageViewAsync Image { get; set; }
            public TextView Title { get; set; }

            public ViewHolder(View rootView)
                : base(rootView)
            {
                Image = rootView.FindViewById<ImageViewAsync>(Resource.Id.pictureImage);
                Title = rootView.FindViewById<TextView>(Resource.Id.txtTitle);
            }
        }
    }
}