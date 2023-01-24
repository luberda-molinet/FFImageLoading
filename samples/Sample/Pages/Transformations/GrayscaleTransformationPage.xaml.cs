using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class GrayscaleTransformationPage : ContentPage
	{
		GrayscaleTransformationPageModel viewModel;

		public GrayscaleTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new GrayscaleTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
