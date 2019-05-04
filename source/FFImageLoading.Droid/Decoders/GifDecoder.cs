using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using FFImageLoading.Work;
using FFImageLoading.Helpers;
using FFImageLoading.Extensions;
using FFImageLoading.Config;
using FFImageLoading.Helpers.Gif;

namespace FFImageLoading.Decoders
{
	public class GifDecoder : IDecoder<Bitmap>
	{
		public async Task<IDecodedImage<Bitmap>> DecodeAsync(Stream stream, string path, ImageSource source, ImageInformation imageInformation, TaskParameter parameters)
		{
			var result = new DecodedImage<Bitmap>();

			using (var gifDecoder = new GifHelper())
			{
				await gifDecoder.ReadAsync(stream, 1);
				gifDecoder.Advance();

				result.IsAnimated = gifDecoder.FrameCount > 1;

				if (result.IsAnimated && Configuration.AnimateGifs)
				{
					result.AnimatedImages = new AnimatedImage<Bitmap>[gifDecoder.FrameCount];

					for (var i = 0; i < gifDecoder.FrameCount; i++)
					{
						var animatedImage = new AnimatedImage<Bitmap>
						{
							Delay = gifDecoder.GetDelay(i),
							Image = await gifDecoder.GetNextFrameAsync()
						};
						result.AnimatedImages[i] = animatedImage;

						gifDecoder.Advance();
					}
				}
				else
				{
					result.IsAnimated = false;
					result.Image = await gifDecoder.GetNextFrameAsync();
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
		}

		public class GifHelper : GifHelperBase<Bitmap>
		{
			protected override Bitmap GetNextBitmap()
			{
				var config = IsFirstFrameTransparent == null || IsFirstFrameTransparent.Value
					? Bitmap.Config.Argb8888 : Bitmap.Config.Rgb565;
				var result = Bitmap.CreateBitmap(DownsampledWidth, DownsampledHeight, config);
				result.HasAlpha = config == Bitmap.Config.Argb8888;
				return result;
			}

			protected override void GetPixels(Bitmap bitmap, int[] pixels, int width, int height)
			{
				bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);
			}

			protected override void Release(Bitmap bitmap)
			{
				bitmap?.Recycle();
			}

			protected override void SetPixels(Bitmap bitmap, int[] pixels, int width, int height)
			{
				bitmap.SetPixels(pixels, 0, width, 0, 0, width, height);
			}
		}


		public Configuration Configuration => ImageService.Instance.Config;

		public IMiniLogger Logger => ImageService.Instance.Config.Logger;
	}
}
