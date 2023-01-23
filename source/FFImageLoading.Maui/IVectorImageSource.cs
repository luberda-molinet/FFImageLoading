using System;
using System.Collections.Generic;
using FFImageLoading.Work;

namespace FFImageLoading.Maui
{
    [Preserve(AllMembers = true)]
	public interface IVectorImageSource
	{
		IVectorDataResolver GetVectorDataResolver();

		Microsoft.Maui.Controls.ImageSource ImageSource { get; }

		int VectorWidth { get; set; }

		int VectorHeight { get; set; }

		bool UseDipUnits { get; set; }

        Dictionary<string, string> ReplaceStringMap { get; set; }

		IVectorImageSource Clone();
	}
}
