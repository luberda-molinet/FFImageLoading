using System;
using System.Collections.Generic;

namespace Sample
{
	public partial class SvgSamplePage : ContentPage
	{
		public SvgSamplePage()
		{
			InitializeComponent();

			BindingContext = new SvgSamplePageModel();
		}
	}
}
