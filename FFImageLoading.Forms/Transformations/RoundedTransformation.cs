using System;

namespace FFImageLoading.Forms.Transformations
{
	public class RoundedTransformation : IFormsTransformation
	{
		public RoundedTransformation(double radius)
		{
			Radius = radius;
		}

		public double Radius { get; private set; }
	}
}

