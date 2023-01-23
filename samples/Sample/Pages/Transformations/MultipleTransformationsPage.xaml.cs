using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class MultipleTransformationsPage : ContentPage
	{
		public MultipleTransformationsPage()
		{
			InitializeComponent();
			BindingContext = new MultipleTransformationsPageModel();
		}
	}
}
