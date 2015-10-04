using System;
using System.Collections.Generic;

namespace FFImageLoading.Forms.Transformations
{
	public class CircleTransformation : IFormsTransformation
	{
		public CircleTransformation()
		{
		}

		#region IFormsTransformation implementation

		public object[] Parameters { get; private set; }

		#endregion
	}
}

