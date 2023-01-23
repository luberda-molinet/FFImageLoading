using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class GrayscaleTransformationPage : ContentPage
	{
		public GrayscaleTransformationPage()
		{
			InitializeComponent();
			BindingContext = new GrayscaleTransformationPageModel();
		}
	}
}
