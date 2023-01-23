using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class RoundedTransformationPage : ContentPage
	{
		public RoundedTransformationPage()
		{
			InitializeComponent();
			BindingContext = new RoundedTransformationPageModel();
		}
	}
}
