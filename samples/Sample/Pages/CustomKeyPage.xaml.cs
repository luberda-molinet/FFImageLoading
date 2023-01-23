using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class CustomKeyPage : ContentPage
	{
		public CustomKeyPage()
		{
			InitializeComponent();
			BindingContext = new CustomKeyPageModel();
		}
	}
}
