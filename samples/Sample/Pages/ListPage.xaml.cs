using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class ListPage : ContentPage
	{
		public ListPage()
		{
			InitializeComponent();
			BindingContext = new ListPageModel();
		}
	}
}
