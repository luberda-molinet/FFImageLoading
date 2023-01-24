using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class CornersTransformationPage : ContentPage
	{
		CornersTransformationPageModel viewModel;

		public CornersTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new CornersTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
