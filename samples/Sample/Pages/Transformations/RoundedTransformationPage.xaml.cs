using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class RoundedTransformationPage : ContentPage
	{
		RoundedTransformationPageModel viewModel;

		public RoundedTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new RoundedTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
