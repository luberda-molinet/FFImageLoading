using System;
using System.Collections.Generic;

namespace Sample.Pages.Transformations
{
    public partial class ColorFillTransformationPage : ContentPage
    {
        ColorFillTransformationPageModel viewModel;

        public ColorFillTransformationPage()
        {
            InitializeComponent();
			BindingContext = viewModel = new ColorFillTransformationPageModel();
        }

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
