using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class RotateTransformationPage : ContentPage
	{
		RotateTransformationPageModel viewModel;

		public RotateTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new RotateTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
