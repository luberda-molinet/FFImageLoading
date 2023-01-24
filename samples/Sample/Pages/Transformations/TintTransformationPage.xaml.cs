using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class TintTransformationPage : ContentPage
	{
		TintTransformationPageModel viewModel;

		public TintTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new TintTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
