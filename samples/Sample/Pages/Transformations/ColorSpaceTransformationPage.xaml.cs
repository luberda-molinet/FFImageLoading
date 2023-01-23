using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class ColorSpaceTransformationPage : ContentPage
	{
		public ColorSpaceTransformationPage()
		{
			InitializeComponent();
			BindingContext = new ColorSpaceTransformationPageModel();
		}
	}
}
