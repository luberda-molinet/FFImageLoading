using System;

namespace FFImageLoading.Forms.Transformations
{
	public class SepiaTransformation : IFormsTransformation
	{
		public SepiaTransformation()
		{

		}

		#region IFormsTransformation implementation

		public object[] Parameters { get; private set; }

		#endregion
	}
}

