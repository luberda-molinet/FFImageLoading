using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class CircleTransformationPage : ContentPage
	{
		CircleTransformationPageModel viewModel;

		public CircleTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new CircleTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
