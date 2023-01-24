using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class MultipleTransformationsPage : ContentPage
	{
		MultipleTransformationsPageModel viewModel;

		public MultipleTransformationsPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new MultipleTransformationsPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
