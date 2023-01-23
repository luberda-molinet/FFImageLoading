using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class TintTransformationPage : ContentPage
	{
		public TintTransformationPage()
		{
			InitializeComponent();
			BindingContext = new TintTransformationPageModel();
		}
	}
}
