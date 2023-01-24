using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class CropTransformationPage : ContentPage
	{
		CropTransformationPageModel viewModel;

		public CropTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new CropTransformationPageModel
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

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
