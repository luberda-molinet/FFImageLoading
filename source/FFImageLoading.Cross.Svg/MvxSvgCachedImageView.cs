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
    public class MvxSvgCachedImageView : MvxCachedImageView
    {
    #if __IOS__
            public MvxSvgCachedImageView() { Initialize(); }
            public MvxSvgCachedImageView(IntPtr handle) : base(handle) { }
            public MvxSvgCachedImageView(CGRect frame) : base(frame) { }
    #elif __ANDROID__
            public MvxSvgCachedImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }
            public MvxSvgCachedImageView(Context context) : base(context) { }
            public MvxSvgCachedImageView(Context context, IAttributeSet attrs) : base(context, attrs) { }
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
    #endif

            if ((!string.IsNullOrWhiteSpace(ImagePath) && ImagePath.Contains("svg", StringComparison.OrdinalIgnoreCase)) || ImageStream != null)
    		{
    			imageLoader.WithCustomDataResolver(new SvgDataResolver(width, height, true, ReplaceStringMap));
    		}
            if (!string.IsNullOrWhiteSpace(LoadingPlaceholderImagePath) && LoadingPlaceholderImagePath.Contains("svg", StringComparison.OrdinalIgnoreCase))
            {
                imageLoader.WithCustomLoadingPlaceholderDataResolver(new SvgDataResolver(width, height, true, ReplaceStringMap));
            }
            if (!string.IsNullOrWhiteSpace(ErrorPlaceholderImagePath) && ErrorPlaceholderImagePath.Contains("svg", StringComparison.OrdinalIgnoreCase))
            {
                imageLoader.WithCustomErrorPlaceholderDataResolver(new SvgDataResolver(width, height, true, ReplaceStringMap));
            }
    	}

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
    }
}
