using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
	public partial class CropTransformationPage : ContentPage, IBasePage<CropTransformationPageModel>
	{
		public CropTransformationPage()
		{
			InitializeComponent();
		}

		public void ReloadImage()
		{
			image.ReloadImage();
			image.LoadingPlaceholder = null;
		}

		void OnPanUpdated(object sender, PanUpdatedEventArgs args)
		{
			this.GetPageModel().OnPanUpdated(args);
		}

		void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs args)
		{
			this.GetPageModel().OnPinchUpdated(args);
		}
	}
}
