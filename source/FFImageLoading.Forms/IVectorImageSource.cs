using System;
using System.Collections.Generic;
using FFImageLoading.Work;

namespace FFImageLoading.Forms
{
    [Preserve(AllMembers = true)]
	public interface IVectorImageSource
	{
		IVectorDataResolver GetVectorDataResolver();

		Xamarin.Forms.ImageSource ImageSource { get; }

		int VectorWidth { get; set; }

		int VectorHeight { get; set; }

		bool UseDipUnits { get; set; }

        Dictionary<string, string> ReplaceStringMap { get; set; }

		IVectorImageSource Clone();
	}
}
