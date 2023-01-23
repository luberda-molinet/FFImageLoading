using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class FlipTransformationPage : ContentPage
	{
		public FlipTransformationPage()
		{
			InitializeComponent();
			BindingContext = new FlipTransformationPageModel();
		}
	}
}
