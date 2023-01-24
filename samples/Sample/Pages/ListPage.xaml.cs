using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class ListPage : ContentPage
	{
		ListPageModel viewModel;

		public ListPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new ListPageModel();
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
