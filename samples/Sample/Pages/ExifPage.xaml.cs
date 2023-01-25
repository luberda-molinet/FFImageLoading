using System;
using System.Collections.Generic;

namespace Sample.Pages
{
	public partial class ExifPage : ContentPage
	{
		ExifPageModel viewModel;

		public ExifPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new ExifPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
		}

	}
}
