using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class FlipTransformation : ITransformation
	{
		public FlipTransformation()
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public FlipTransformation(FlipType flipType)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

        public FlipType FlipType { get; set; }

		public string Key
		{
			get
			{
				throw new Exception(Common.DoNotReferenceMessage);
			}
		}
	}
}

