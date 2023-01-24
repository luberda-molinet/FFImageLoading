using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class SepiaTransformationPage : ContentPage
	{
		SepiaTransformationPageModel viewModel;

		public SepiaTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new SepiaTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
