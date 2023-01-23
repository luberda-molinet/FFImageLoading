using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class ListTransformationsPage : ContentPage
	{
		public ListTransformationsPage()
		{
			InitializeComponent();
			BindingContext = new ListTransformationsPageModel();
		}
	}
}
