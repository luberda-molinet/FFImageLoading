using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample
{
	public partial class BlurredTransformationPage : ContentPage
	{
		BlurredTransformationPageModel viewModel;

		public BlurredTransformationPage()
		{
			InitializeComponent();

			BindingContext = viewModel = new BlurredTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
