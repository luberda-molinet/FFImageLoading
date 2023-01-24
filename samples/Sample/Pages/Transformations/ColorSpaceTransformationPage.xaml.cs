using System;
using System.Collections.Generic;
//using AndroidX.Lifecycle;
using FFImageLoading.Transformations;

namespace Sample
{
	public partial class ColorSpaceTransformationPage : ContentPage
	{
		ColorSpaceTransformationPageModel viewModel;

		public ColorSpaceTransformationPage()
		{
			InitializeComponent();
			BindingContext = viewModel = new ColorSpaceTransformationPageModel();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			image.Transformations.Add(new ColorSpaceTransformation
			{
				RGBAWMatrix = FFColorMatrix.InvertColorMatrix
			});

			viewModel.Reload();
		}
	}
}
