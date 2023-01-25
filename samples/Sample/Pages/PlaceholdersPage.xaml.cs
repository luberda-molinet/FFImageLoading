using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class PlaceholdersPage : ContentPage
	{
		PlaceholdersPageModel viewModel;
		public PlaceholdersPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new PlaceholdersPageModel();
		}

	}
}
