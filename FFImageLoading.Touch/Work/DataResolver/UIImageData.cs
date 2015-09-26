using System;
using FFImageLoading.Work;

namespace FFImageLoading.Work.DataResolver
{
	public class UIImageData
	{
		public byte[] Data { get; set; }
		public LoadingResult Result { get; set; }
		public string ResultIdentifier { get; set; }
	}
}

