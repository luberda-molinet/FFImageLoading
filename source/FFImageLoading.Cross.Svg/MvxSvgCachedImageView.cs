using System;
using System.Collections.Generic;
using FFImageLoading.Work;
using FFImageLoading.Cache;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using FFImageLoading.Views;
using FFImageLoading.Svg.Platform;

#if __IOS__
using Foundation;
using UIKit;
using CoreGraphics;
#elif __ANDROID__
using Android.Util;
using Android.Runtime;
using Android.Content;
#endif

namespace FFImageLoading.Cross
{
    #if __IOS__
            [Preserve(AllMembers = true)]
            [Register("MvxSvgCachedImageView")]
    #elif __ANDROID__
            [Preserve(AllMembers = true)]
            [Register("ffimageloading.cross.MvxSvgCachedImageView")]
    #endif
    /// <summary>
    /// MvxSvgCachedImageView by Daniel Luberda
    /// </summary>
    public class MvxSvgCachedImageView : MvxCachedImageView
    {
#if __IOS__
        public MvxSvgCachedImageView() : base() { } 
        public MvxSvgCachedImageView(IntPtr handle) : base(handle) { }
        public MvxSvgCachedImageView(CGRect frame) : base(frame) { }
#elif __ANDROID__
        public MvxSvgCachedImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }
        public MvxSvgCachedImageView(Context context) : base(context) { }
        public MvxSvgCachedImageView(Context context, IAttributeSet attrs) : base(context, attrs) { }

#elif __WINDOWS__
        public MvxSvgCachedImageView() : base() { }
#endif

#if !__WINDOWS__
        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(ReplaceStringMap))
            {
                if (_lastImageSource != null)
                {
                    UpdateImageLoadingTask();
                }
            }
        }

#endif

        protected override void SetupOnBeforeImageLoading(TaskParameter imageLoader)
        {
            base.SetupOnBeforeImageLoading(imageLoader);

#if __IOS__
            int width = (int)this.Bounds.Width;
            int height = (int)this.Bounds.Height;
#elif __ANDROID__
            int width = this.Width;
            int height = this.Height;
#elif __WINDOWS__
            int width = (int)this.Width;
            int height = (int)this.Height;                
#endif

            if (width > height)
                height = 0;
            else
                width = 0;

            if ((!string.IsNullOrWhiteSpace(ImagePath) && ImagePath.IsSvgFileUrl()) || ImageStream != null)
            {
                imageLoader.WithCustomDataResolver(new SvgDataResolver(width, height, true, ReplaceStringMap));
            }
            if (!string.IsNullOrWhiteSpace(LoadingPlaceholderImagePath) && LoadingPlaceholderImagePath.IsSvgFileUrl())
            {
                imageLoader.WithCustomLoadingPlaceholderDataResolver(new SvgDataResolver(width, height, true, ReplaceStringMap));
            }
            if (!string.IsNullOrWhiteSpace(ErrorPlaceholderImagePath) && ErrorPlaceholderImagePath.IsSvgFileUrl())
            {
                imageLoader.WithCustomErrorPlaceholderDataResolver(new SvgDataResolver(width, height, true, ReplaceStringMap));
            }
        }

#if __WINDOWS__
        public Dictionary<string, string> ReplaceStringMap { get { return (Dictionary<string, string>)GetValue(ReplaceStringMapProperty); } set { SetValue(ReplaceStringMapProperty, value); } }
        public static readonly Windows.UI.Xaml.DependencyProperty ReplaceStringMapProperty = Windows.UI.Xaml.DependencyProperty.Register(nameof(ReplaceStringMap), typeof(Dictionary<string, string>), typeof(MvxCachedImageView), new Windows.UI.Xaml.PropertyMetadata(default(Dictionary<string, string>), OnImageChanged));
#else
        Dictionary<string, string> _replaceStringMap;
        /// <summary>
        /// Used to define replacement map which will be used to
        /// replace text inside SVG file (eg. changing colors values)
        /// </summary>
        /// <value>The replace string map.</value>
        public Dictionary<string, string> ReplaceStringMap
        {
            get { return _replaceStringMap; }
            set { if (_replaceStringMap != value) { _replaceStringMap = value; OnPropertyChanged(nameof(ReplaceStringMap)); } }
        }
#endif
    }
}
