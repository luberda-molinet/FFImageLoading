using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class MenuPage : ContentPage
	{
		public MenuPage()
		{
			InitializeComponent();
			BindingContext = new MenuPageModel();
		}
	}
}
