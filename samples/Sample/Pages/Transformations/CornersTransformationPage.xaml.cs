using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class CornersTransformationPage : ContentPage
	{
		public CornersTransformationPage()
		{
			InitializeComponent();
			BindingContext = new CornersTransformationPageModel();
		}
	}
}
