using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class ListTransformationsPage : ContentPage
	{
		ListTransformationsPageModel viewModel;

		public ListTransformationsPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new ListTransformationsPageModel();
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
