using System;

using UIKit;

using FFImageLoading;
using FFImageLoading.Work;
using FFImageLoading.Transformations;

namespace Simple.iOS.Sample
{
    public partial class ImageDetailsViewController : UIViewController
    {
        public const string SegueImageDetails = "ImageDetails-segue";

        public string imageURL = string.Empty;

        public ImageDetailsViewController(IntPtr handle)
            : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            transformation = 0;
            LoadImage();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            imageURL = string.Empty;
        }

        int transformation = 0;

        protected void LoadImage()
        {
            imageView.Image = null;

            var taskImage = ImageService.Instance.LoadUrl(imageURL)
                                        .ErrorPlaceholder("error.png", ImageSource.ApplicationBundle)
                                        .LoadingPlaceholder("placeholder", ImageSource.CompiledResource);
            if(transformation==0)
            {
                taskImage.Into(imageView);
                transformation++;
            }
            else if(transformation==1)
            {
                taskImage.Transform(new CircleTransformation())
                         .Into(imageView);
                transformation++;
            }
            else if(transformation==2)
            {
                taskImage.Transform(new RoundedTransformation(10))
                         .Into(imageView);
                transformation = 0;
            }
        }

        partial void TapTransformation (Foundation.NSObject sender)
        {
            LoadImage();
        }

    }
}


