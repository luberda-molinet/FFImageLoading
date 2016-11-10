using System;
using FFImageLoading.Work;

namespace FFImageLoading.Forms
{
	public interface IVectorImageSource
	{
		IVectorDataResolver GetVectorDataResolver();

		Xamarin.Forms.ImageSource ImageSource { get; }

		int VectorWidth { get; set; }

		int VectorHeight { get; set; }

		bool UseDipUnits { get; set; }
	}
}
