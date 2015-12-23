using System;

using Foundation;
using UIKit;
using FFImageLoading;
using FFImageLoading.Work;
using FFImageLoading.Transformations;

namespace Simple.iOS.Sample
{
    public partial class ImageViewCell : UICollectionViewCell
    {
        public static readonly NSString Key = new NSString("ImageViewCell");

        private string _imageURL;
        public string imageURL
        {
            set 
            {
                if(value!=_imageURL)
                {
                    _imageURL = value;
                    UpdateContent();
                }                
            }
            get { return _imageURL; }
        }

        public ImageViewCell(IntPtr handle)
            : base(handle)
        { }

        protected void UpdateContent()
        {
            ImageService.LoadUrl(imageURL)
                        .ErrorPlaceholder("error@2x.png", ImageSource.ApplicationBundle)
                        .LoadingPlaceholder("placeholder.png", ImageSource.CompiledResource)
                        .Into(imageView);            
        }
    }
}
