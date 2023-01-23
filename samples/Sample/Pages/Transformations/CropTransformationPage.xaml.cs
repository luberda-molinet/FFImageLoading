using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class CropTransformationPage : ContentPage
	{
		public CropTransformationPage()
		{
			InitializeComponent();
			BindingContext = new CropTransformationPageModel
			{
				ReloadImageHandler = () => ReloadImage()
			};
		}

		public void ReloadImage()
		{
			image.ReloadImage();
			image.LoadingPlaceholder = null;
		}

		void OnPanUpdated(object sender, PanUpdatedEventArgs args)
		{
			(this.BindingContext as CropTransformationPageModel)?.OnPanUpdated(args);
		}

		void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs args)
		{
			(this.BindingContext as CropTransformationPageModel)?.OnPinchUpdated(args);
		}
	}
}
