using System;
using System.Collections.Generic;

namespace Sample
{
    public partial class TransformationsSelectorPage : ContentPage
    {
        TransformationsSelectorPageModel viewModel;

        public TransformationsSelectorPage()
        {
            InitializeComponent();
			BindingContext = viewModel = new TransformationsSelectorPageModel();

		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
