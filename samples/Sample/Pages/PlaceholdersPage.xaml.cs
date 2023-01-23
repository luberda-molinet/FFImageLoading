using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class PlaceholdersPage : ContentPage
	{
		public PlaceholdersPage()
		{
			InitializeComponent();
			BindingContext = new PlaceholdersPageModel();
		}
	}
}
