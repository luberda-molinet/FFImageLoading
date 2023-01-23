using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class SepiaTransformationPage : ContentPage
	{
		public SepiaTransformationPage()
		{
			InitializeComponent();
			BindingContext = new SepiaTransformationPageModel();
		}
	}
}
