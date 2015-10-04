using System;
using System.Collections.Generic;

namespace FFImageLoading.Forms.Transformations
{
	public class RoundedTransformation : IFormsTransformation
	{
		public RoundedTransformation(double radius)
		{
			Parameters = new object[]{
				radius
			};
		}

		#region IFormsTransformation implementation

		public object[] Parameters { get; private set; }

		#endregion
	}
}

