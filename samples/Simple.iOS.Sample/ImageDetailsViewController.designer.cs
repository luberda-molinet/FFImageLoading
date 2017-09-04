// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Simple.iOS.Sample
{
	[Register ("ImageDetailsViewController")]
	partial class ImageDetailsViewController
	{
		[Outlet]
		UIKit.UIImageView imageView { get; set; }

		[Action ("TapTransformation:")]
		partial void TapTransformation (Foundation.NSObject sender);

		void ReleaseDesignerOutlets ()
		{
			if (imageView != null) {
				imageView.Dispose ();
				imageView = null;
			}
		}
	}
}
