using System;
using System.Collections.Generic;

namespace FFImageLoading.Work
{
	public interface IMultiplatformTransformation : ITransformation
	{
		void SetParameters(object[] parameters);
	}
}

