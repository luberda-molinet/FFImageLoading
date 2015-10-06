using System;

namespace FFImageLoading.Forms.Transformations
{
	public class BlurredTransformation : IFormsTransformation
	{
		public BlurredTransformation(double radius)
		{
			Parameters = new object[]{
				radius
			};
		}

		#region IFormsTransformation implementation

		public object[] Parameters { get; private set;}

		#endregion
	}
}

