using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample
{
	public partial class BlurredTransformationPage : ContentPage
	{
		public BlurredTransformationPage()
		{
			InitializeComponent();

			BindingContext = new BlurredTransformationPageModel();
		}
	}
}
