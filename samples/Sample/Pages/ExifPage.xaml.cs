using System;
using System.Collections.Generic;

namespace Sample.Pages
{
	public partial class ExifPage : ContentPage
	{
		public ExifPage()
		{
			InitializeComponent();
			BindingContext = new ExifPageModel();
		}
	}
}
