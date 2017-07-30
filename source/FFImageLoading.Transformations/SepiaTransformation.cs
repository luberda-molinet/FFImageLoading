using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
	public class SepiaTransformation : ITransformation
	{
        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}
		public string Key
		{
			get
			{
				throw new Exception(Common.DoNotReferenceMessage);
			}
		}
	}
}

