using System;
using FFImageLoading.Work;
using UIKit;

namespace FFImageLoading.Work.DataResolver
{
	public class UIImageData
	{
		public byte[] Data { get; set; }

		public UIImage Image { get; set; }

		public LoadingResult Result { get; set; }

		public ImageInformation ImageInformation { get; set; }

		public string ResultIdentifier { get; set; }
	}
}

