using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class CircleTransformationPage : ContentPage
	{
		public CircleTransformationPage()
		{
			InitializeComponent();
			BindingContext = new CircleTransformationPageModel();
		}
	}
}
