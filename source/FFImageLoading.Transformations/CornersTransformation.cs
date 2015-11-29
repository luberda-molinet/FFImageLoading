using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class CornersTransformation : ITransformation
	{
		private const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";

		public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType)
		{
			throw new Exception(DoNotReference);
		}

		public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize, 
			CornerTransformType cornersTransformType)
		{
			throw new Exception(DoNotReference);
		}

		public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
		{
			throw new Exception(DoNotReference);
		}

		public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize, 
			CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
		{
			throw new Exception(DoNotReference);
		}

		public IBitmap Transform(IBitmap source)
		{
			throw new Exception(DoNotReference);
		}

		public string Key
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}
	}
}

