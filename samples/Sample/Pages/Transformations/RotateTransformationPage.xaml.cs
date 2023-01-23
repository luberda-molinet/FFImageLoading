using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class RotateTransformationPage : ContentPage
	{
		public RotateTransformationPage()
		{
			InitializeComponent();
			BindingContext = new RotateTransformationPageModel();
		}
	}
}
