using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class CustomKeyPage : ContentPage
	{
		CustomKeyPageModel viewModel;

		public CustomKeyPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new CustomKeyPageModel();
		}

		void ListView_ItemSelected(System.Object sender, Microsoft.Maui.Controls.SelectedItemChangedEventArgs e)
		{
			viewModel.ItemSelected();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			viewModel.Reload();
		}
	}
}
