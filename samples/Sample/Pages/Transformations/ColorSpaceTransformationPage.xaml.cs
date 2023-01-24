using System;
using System.Collections.Generic;
//using AndroidX.Lifecycle;
using FFImageLoading.Transformations;

namespace Sample
{
	public partial class ColorSpaceTransformationPage : ContentPage
	{
		public ColorSpaceTransformationPage()
		{
			InitializeComponent();
			BindingContext = new ColorSpaceTransformationPageModel();

			image.Transformations.Add(new ColorSpaceTransformation
			{
				RGBAWMatrix = FFColorMatrix.InvertColorMatrix
			});
			//< ffimageloading:CachedImage.Transformations >

			//			< fftransformations:ColorSpaceTransformation RGBAWMatrix = "{x:Static ff:FFColorMatrix.InvertColorMatrix}" />

			//		</ ffimageloading:CachedImage.Transformations >
		}
	}
}
