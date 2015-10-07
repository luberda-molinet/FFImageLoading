using System;
using FFImageLoading;
using System.Linq;

namespace FFImageLoading.Forms.Transformations
{
	public class ColorSpaceTransformation : IFormsTransformation
	{
		public ColorSpaceTransformation(float[][] rgbawMatrix)
		{
			if (rgbawMatrix.Length != 5 || rgbawMatrix.Any(v => v.Length != 5))
				throw new ArgumentException("Wrong size of RGBAW color matrix");

			Parameters = new object[] { rgbawMatrix };
		}

		#region IFormsTransformation implementation

		public object[] Parameters { get; private set; }

		#endregion
	}
}

