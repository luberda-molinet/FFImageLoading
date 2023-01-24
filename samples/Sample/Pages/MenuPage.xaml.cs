using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class MenuPage : ContentPage
	{
		MenuPageModel viewModel;

		public MenuPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new MenuPageModel(this.Navigation);
		}

		void ListView_ItemSelected(System.Object sender, Microsoft.Maui.Controls.SelectedItemChangedEventArgs e)
		{
			viewModel.ItemSelected();
		}
	}
}
