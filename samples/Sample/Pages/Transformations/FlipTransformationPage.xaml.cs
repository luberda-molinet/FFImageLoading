using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class FlipTransformationPage : ContentPage
	{
		FlipTransformationPageModel viewModel;

		public FlipTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new FlipTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
