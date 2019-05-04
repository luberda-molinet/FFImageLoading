using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using FFImageLoading.Work;
using FFImageLoading.Helpers;
using FFImageLoading.Extensions;
using FFImageLoading.Config;
using BumpTech.GlideLib.GifDecoderLib;

namespace FFImageLoading.Decoders
{
	public class GifDecoder : IDecoder<Bitmap>
	{
		public async Task<IDecodedImage<Bitmap>> DecodeAsync(Stream stream, string path, ImageSource source, ImageInformation imageInformation, TaskParameter parameters)
		{
			await Task.Yield();

			var result = new DecodedImage<Bitmap>();

			var bitMapProvider = new BitmapProvider();
			var gifDecoder = new StandardGifDecoder(bitMapProvider);

			var bytes = stream.ToBytes();
			gifDecoder.Read(bytes);
			gifDecoder.Advance();

			result.IsAnimated = gifDecoder.FrameCount > 1;

			if (result.IsAnimated && Configuration.AnimateGifs)
			{
				result.AnimatedImages = new AnimatedImage<Bitmap>[gifDecoder.FrameCount];

				for (var i = 0; i < gifDecoder.FrameCount; i++)
				{
					var animatedImage = new AnimatedImage<Bitmap>();
					animatedImage.Delay = gifDecoder.GetDelay(i);
					animatedImage.Image = gifDecoder.NextFrame;
					result.AnimatedImages[i] = animatedImage;

					gifDecoder.Advance();
				}
			}
			else
			{
				result.IsAnimated = false;
				result.Image = gifDecoder.NextFrame;
			}



			if (result.Image != null)
			{
				imageInformation.SetOriginalSize(result.Image.Width, result.Image.Height);
				imageInformation.SetCurrentSize(result.Image.Width, result.Image.Height);
			}
			else if (result.AnimatedImages != null)
			{
				if (result.AnimatedImages.Length > 0)
				{
					if (result.AnimatedImages[0].Image != null)
					{
						imageInformation.SetOriginalSize(result.AnimatedImages[0].Image.Width, result.AnimatedImages[0].Image.Height);
						imageInformation.SetCurrentSize(result.AnimatedImages[0].Image.Width, result.AnimatedImages[0].Image.Height);
					}
				}
			}

			return result;
		}




		public Configuration Configuration => ImageService.Instance.Config;

		public IMiniLogger Logger => ImageService.Instance.Config.Logger;

		private class BitmapProvider : Java.Lang.Object, IGifDecoderBitmapProvider
		{
			public Bitmap Obtain(int width, int height, Bitmap.Config config)
			{
				return Bitmap.CreateBitmap(width, height, config);
			}

			public byte[] ObtainByteArray(int size)
			{
				return new byte[size];
			}

			public int[] ObtainIntArray(int size)
			{
				return new int[size];
			}

			public void Release(Bitmap bitmap)
			{
				bitmap.Recycle();
			}

			public void Release(byte[] bytes)
			{
				//nothing to do 
			}

			public void Release(int[] array)
			{
				//nothing to do 
			}
		}
	}
}
