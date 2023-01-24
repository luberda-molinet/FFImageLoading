using System;
using System.Collections.Generic;

namespace Sample
{
    public partial class BasicPage : ContentPage
    {
        BasicPageModel viewModel;

		public BasicPage()
        {
            InitializeComponent();
			BindingContext = viewModel = new BasicPageModel();

		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
