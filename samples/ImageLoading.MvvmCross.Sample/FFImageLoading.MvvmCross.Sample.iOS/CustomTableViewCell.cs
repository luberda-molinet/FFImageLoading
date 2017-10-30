using Foundation;
using System;
using UIKit;
using MvvmCross.Binding.iOS.Views;
using MvvmCross.Binding.BindingContext;
using FFImageLoading.MvvmCross.Sample.Core;
using FFImageLoading.Cross;
using Cirrious.FluentLayouts.Touch;

namespace FFImageLoading.MvvmCross.Sample.iOS
{
    public partial class CustomTableViewCell : MvxTableViewCell
    {
        private MvxCachedImageView _imageControl;

        private bool _constraintsCreated;

        public CustomTableViewCell(IntPtr handle) : base(handle)
        {
            _imageControl = new MvxCachedImageView();
            _imageControl.BackgroundColor = UIColor.Red;

            ContentView.AddSubview(_imageControl);
            ContentView.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();

            SetNeedsUpdateConstraints();

            this.DelayBind(
                () =>
                {
                    _imageControl.LoadingPlaceholderImagePath = "res:place.jpg";
                    _imageControl.ErrorPlaceholderImagePath = "res:place.jpg";
                    _imageControl.TransformPlaceholders = true;

                    var set = this.CreateBindingSet<CustomTableViewCell, Image>();
                    set.Bind(_imageControl).For(v => v.DownsampleWidth).To(vm => vm.DownsampleWidth);
                    set.Bind(_imageControl).For(v => v.Transformations).To(vm => vm.Transformations);
                    set.Bind(_imageControl).For(v => v.ImagePath).To(vm => vm.Url);
                    set.Apply();
                }
            );
        }

        public override void UpdateConstraints()
        {
            if (!_constraintsCreated)
            {
                ContentView.AddConstraints(
                    _imageControl.WithSameCenterY(ContentView),
                    _imageControl.WithSameCenterX(ContentView),
                    _imageControl.Width().EqualTo(55f),
                    _imageControl.Height().EqualTo(55f)
                );

                _constraintsCreated = true;
            }

            base.UpdateConstraints();
        }
    }
}
