using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class StreamListPage : ContentPage
	{
		StreamListPageModel viewModel;

		public StreamListPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new StreamListPageModel();
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
