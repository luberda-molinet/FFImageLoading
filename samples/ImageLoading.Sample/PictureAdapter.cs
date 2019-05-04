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
using Android.Support.V4.Util;
using Android.Support.V4.App;

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
            view.Click += view_Click;
            return new ViewHolder(view);
        }

        void view_Click(object sender, EventArgs e)
        {
            var position = (int)((View)sender).Tag;

            var view = (View)sender;
            var pictureImage = view.FindViewById<ImageView>(Resource.Id.pictureImage);
            var txtTitle = view.FindViewById<TextView>(Resource.Id.txtTitle);

            Intent intent = new Intent(context, typeof(DetailActivity));
            intent.PutExtra(DetailActivity.POSITION, position);

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                var p1 = Pair.Create(pictureImage, pictureImage.TransitionName);
                var p2 = Pair.Create(txtTitle, txtTitle.TransitionName);

                var options = ActivityOptionsCompat.MakeSceneTransitionAnimation(context, p1, p2);
                context.StartActivity(intent, options.ToBundle());
            }
            else
            {
                context.StartActivity(intent);
            }
        }

        // Replace the contents of a view (invoked by the layout manager)
        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var item = Items[position];

            var vh = viewHolder as ViewHolder;

            vh.Title.Text = position.ToString();
            vh.ItemView.Tag = position;

            ImageService.Instance.LoadUrl(item)
               .Retry(3, 200)
               .DownSample(100, 100)
               .LoadingPlaceholder(Config.LoadingPlaceholderPath, FFImageLoading.Work.ImageSource.ApplicationBundle)
               .ErrorPlaceholder(Config.ErrorPlaceholderPath, FFImageLoading.Work.ImageSource.ApplicationBundle)
               .Into(vh.Image);
        }

        public class ViewHolder : RecyclerView.ViewHolder
        {
            public ImageView Image { get; set; }
            public TextView Title { get; set; }

            public ViewHolder(View rootView)
                : base(rootView)
            {
                Image = rootView.FindViewById<ImageView>(Resource.Id.pictureImage);
                Title = rootView.FindViewById<TextView>(Resource.Id.txtTitle);
            }
        }
    }
}
