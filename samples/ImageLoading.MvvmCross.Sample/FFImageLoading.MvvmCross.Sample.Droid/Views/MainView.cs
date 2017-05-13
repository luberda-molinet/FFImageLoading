using System;
using Android.App;
using Android.OS;

namespace FFImageLoading.MvvmCross.Sample.Droid
{
    [Activity(Label = "View for MainView", MainLauncher = true)]
    public class MainView : BaseView
    {
        /// <summary>
        /// Called when [create].
        /// </summary>
        /// <param name="bundle">The bundle.</param>
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
        }

        protected override void OnViewModelSet()
        {
            base.OnViewModelSet();
            this.SetContentView(Resource.Layout.MainView);
        }
    }
}
