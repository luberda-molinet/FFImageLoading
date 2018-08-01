using System;

using UIKit;
using Foundation;
using FFImageLoading;
using FFImageLoading.Work;
using System.Threading.Tasks;

namespace Simple.iOS.Sample
{
    public partial class ImagesViewController : UICollectionViewController
    {
        protected string currentURL = string.Empty;

        public ImagesViewController(IntPtr handle)
            : base(handle)
        {}

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            currentURL = string.Empty;
            CollectionView.ReloadData();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            base.PrepareForSegue(segue, sender);
            if (segue.Identifier == ImageDetailsViewController.SegueImageDetails)
            {
                var details = (ImageDetailsViewController)segue.DestinationViewController;
                details.imageURL = currentURL;
            }
        }

        public override nint NumberOfSections(UICollectionView collectionView)
        { return 1; }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            return Config.Images.Count;
        }

        public override UICollectionViewCell GetCell (UICollectionView collectionView, Foundation.NSIndexPath indexPath)
        {
            var imageCell = (ImageViewCell)collectionView.DequeueReusableCell (ImageViewCell.Key, indexPath);
            imageCell.imageURL = Config.Images[indexPath.Row];
            return imageCell;
        }

        public override void ItemSelected(UICollectionView collectionView, Foundation.NSIndexPath indexPath)
        {
            collectionView.DeselectItem(indexPath, false);
            ImageSelected(indexPath.Row);
        }

        protected void ImageSelected(int imageIndex)
        {
            currentURL = Config.Images[imageIndex];
            PerformSegue(ImageDetailsViewController.SegueImageDetails, this);
        }

        partial void TapReloadAll (Foundation.NSObject sender)
        {
            Task.Run(async () =>
            {
                ImageService.Instance.InvalidateMemoryCache();
                await ImageService.Instance.InvalidateDiskCacheAsync();
                CollectionView.ReloadData();
            });
        }
    }
}

