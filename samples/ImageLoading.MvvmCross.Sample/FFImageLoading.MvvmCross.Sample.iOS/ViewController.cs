using System;
using FFImageLoading.MvvmCross.Sample.Core;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS.Views;
using MvvmCross.iOS.Views;
using UIKit;

namespace FFImageLoading.MvvmCross.Sample.iOS
{
    public partial class ViewController : MvxTableViewController<MainViewModel>
    {
        public ViewController()
        {

        }

        protected ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var source = new MvxSimpleTableViewSource(TableView, typeof(CustomTableViewCell), nameof(CustomTableViewCell));
            TableView.Source = source;
            TableView.RowHeight = 80f;

            var set = this.CreateBindingSet<ViewController, MainViewModel>();
            set.Bind(source).For(v => v.ItemsSource).To(vm => vm.Images);
            set.Apply();
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
        }
    }
}
