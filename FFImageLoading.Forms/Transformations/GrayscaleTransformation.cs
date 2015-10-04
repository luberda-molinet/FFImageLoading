using System;
using System.Collections.Generic;

namespace FFImageLoading.Forms.Transformations
{
	public class GrayscaleTransformation : IFormsTransformation
	{
		public GrayscaleTransformation()
		{
			
		}

		#region IFormsTransformation implementation

		public object[] Parameters { get; private set; }

		#endregion
	}
}

