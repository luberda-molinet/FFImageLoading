using System;
using FFImageLoading.Work;
using FFImageLoading.Forms.Transformations;

namespace FFImageLoading.Forms.Droid
{
	internal static class TransformationBinding
	{
		internal static ITransformation GetNativeTransformation(IFormsTransformation transformation)
		{
			// GrayscaleTransformation
			var grayscaleTransformation = transformation as GrayscaleTransformation;
			if (grayscaleTransformation != null)
			{
				return new FFImageLoading.Transformations.GrayscaleTransformation();
			}

			// CircleTransformation
			var circleTransformation = transformation as CircleTransformation;
			if (circleTransformation != null)
			{
				return new FFImageLoading.Transformations.CircleTransformation();
			}

			// RoundedTransformation
			var roundedTransformation = transformation as RoundedTransformation;
			if (roundedTransformation != null)
			{
				return new FFImageLoading.Transformations.RoundedTransformation(roundedTransformation.Radius);
			}

			throw new NotImplementedException("This transformation is not supported by FFImageLoading.Forms renderer.");
		}
	}
}

