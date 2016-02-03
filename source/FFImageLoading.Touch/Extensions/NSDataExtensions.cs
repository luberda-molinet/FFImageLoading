using System;
using UIKit;
using Foundation;
using CoreGraphics;
using ImageIO;
using FFImageLoading.Helpers;

namespace FFImageLoading.Extensions
{
	public static class NSDataExtensions
	{
		public enum RCTResizeMode: long
		{
			ScaleAspectFill = UIViewContentMode.ScaleAspectFill,
			ScaleAspectFit = UIViewContentMode.ScaleAspectFit,
			ScaleToFill = UIViewContentMode.ScaleToFill,
		}

		// Shamelessly copied from React-Native: https://github.com/facebook/react-native/blob/2cbc9127560c5f0f89ae5aa6ff863b1818f1c7c3/Libraries/Image/RCTImageUtils.m
		public static UIImage ToImage(this NSData data, CGSize destSize, nfloat destScale, RCTResizeMode resizeMode = RCTResizeMode.ScaleAspectFit)
		{
			using (var sourceRef = CGImageSource.FromData(data))
			{
				if (sourceRef == null)
				{
					return null;
				}

				// Get original image size
				var imageProperties = sourceRef.GetProperties(0);

				if (imageProperties == null)
				{
					return null;
				}

				var sourceSize = new CGSize((nfloat)imageProperties.PixelWidth, (nfloat)imageProperties.PixelHeight);

				if (destSize.IsEmpty)
				{
					destSize = sourceSize;
					if (destScale <= 0)
					{
						destScale = 1;
					}
				}
				else if (destScale <= 0)
				{
					destScale = ScaleHelper.Scale;
				}

				// Calculate target size
				CGSize targetSize = RCTTargetSize(sourceSize, 1, destSize, destScale, resizeMode, false);
				CGSize targetPixelSize = RCTSizeInPixels(targetSize, destScale);
				int maxPixelSize = (int)Math.Max(
					Math.Min(sourceSize.Width, targetPixelSize.Width),
					Math.Min(sourceSize.Height, targetPixelSize.Height)
				);

				var options = new CGImageThumbnailOptions()
				{
						ShouldAllowFloat = true,
						CreateThumbnailWithTransform = true,
						CreateThumbnailFromImageAlways = true,
						MaxPixelSize = maxPixelSize
				};

				// Get thumbnail
				using (var imageRef = sourceRef.CreateThumbnail(0, options))
				{
					if (imageRef == null)
					{
						return null;
					}

					// Return image
					var image = new UIImage(imageRef, destScale, UIImageOrientation.Up);
					return image;
				}
			}
		}

		private static CGSize RCTTargetSize(CGSize sourceSize, nfloat sourceScale,
			CGSize destSize, nfloat destScale,
			RCTResizeMode resizeMode,
			bool allowUpscaling)
		{
			switch (resizeMode)
			{
				case RCTResizeMode.ScaleToFill:

					if (!allowUpscaling)
					{
						nfloat scale = sourceScale / destScale;
						destSize.Width = (nfloat)Math.Min(sourceSize.Width * scale, destSize.Width);
						destSize.Height = (nfloat)Math.Min(sourceSize.Height * scale, destSize.Height);
					}
					return RCTCeilSize(destSize, destScale);

				default: {

						// Get target size
						CGSize size = RCTTargetRect(sourceSize, destSize, destScale, resizeMode).Size;
						if (!allowUpscaling)
						{
							// return sourceSize if target size is larger
							if (sourceSize.Width * sourceScale < size.Width * destScale)
							{
								return sourceSize;
							}
						}
						return size;
					}
			}
		}

		private static CGSize RCTSizeInPixels(CGSize pointSize, nfloat scale)
		{
			return new CGSize(
				Math.Ceiling(pointSize.Width * scale),
				Math.Ceiling(pointSize.Height * scale)
			);
		}

		private static CGSize RCTCeilSize(CGSize size, nfloat scale)
		{
			return new CGSize(
				RCTCeilValue(size.Width, scale),
				RCTCeilValue(size.Height, scale)
			);
		}

		private static nfloat RCTCeilValue(nfloat value, nfloat scale)
		{
			return (nfloat)Math.Ceiling(value * scale) / scale;
		}

		private static nfloat RCTFloorValue(nfloat value, nfloat scale)
		{
			return (nfloat)Math.Floor(value * scale) / scale;
		}

		private static CGRect RCTTargetRect(CGSize sourceSize, CGSize destSize, nfloat destScale, RCTResizeMode resizeMode)
		{
			if (destSize.IsEmpty)
			{
				// Assume we require the largest size available
				return new CGRect(CGPoint.Empty, sourceSize);
			}

			nfloat aspect = sourceSize.Width / sourceSize.Height;
			// If only one dimension in destSize is non-zero (for example, an Image
			// with `flex: 1` whose height is indeterminate), calculate the unknown
			// dimension based on the aspect ratio of sourceSize
			if (destSize.Width == 0)
			{
				destSize.Width = destSize.Height * aspect;
			}
			if (destSize.Height == 0)
			{
				destSize.Height = destSize.Width / aspect;
			}

			// Calculate target aspect ratio if needed (don't bother if resizeMode == scale to fill)
			nfloat targetAspect = 0.0f;
			if (resizeMode != RCTResizeMode.ScaleToFill)
			{
				targetAspect = destSize.Width / destSize.Height;
				if (aspect == targetAspect)
				{
					resizeMode = RCTResizeMode.ScaleToFill;
				}
			}

			switch (resizeMode)
			{
				case RCTResizeMode.ScaleToFill:
					return new CGRect(CGPoint.Empty, RCTCeilSize(destSize, destScale));

				case RCTResizeMode.ScaleAspectFit:
					if (targetAspect <= aspect) // target is taller than content
					{
						sourceSize.Width = destSize.Width = destSize.Width;
						sourceSize.Height = sourceSize.Width / aspect;

					}
					else // target is wider than content
					{
						sourceSize.Height = destSize.Height = destSize.Height;
						sourceSize.Width = sourceSize.Height * aspect;
					}

					return new CGRect(
						new CGPoint(
							RCTFloorValue((destSize.Width - sourceSize.Width) / 2, destScale),
							RCTFloorValue((destSize.Height - sourceSize.Height) / 2, destScale)
						),
						RCTCeilSize(sourceSize, destScale)
					);
				
				case RCTResizeMode.ScaleAspectFill:
				default:
					if (targetAspect <= aspect) { // target is taller than content

						sourceSize.Height = destSize.Height = destSize.Height;
						sourceSize.Width = sourceSize.Height * aspect;
						destSize.Width = destSize.Height * targetAspect;
						return new CGRect(
							new CGPoint(
								RCTFloorValue((destSize.Width - sourceSize.Width) / 2, destScale),
								0
							),
							RCTCeilSize(sourceSize, destScale)
						);
					}
					else // target is wider than content
					{
						sourceSize.Width = destSize.Width = destSize.Width;
						sourceSize.Height = sourceSize.Width / aspect;
						destSize.Height = destSize.Width / targetAspect;
						return new CGRect(
							new CGPoint(
								0,
								RCTFloorValue((destSize.Height - sourceSize.Height) / 2, destScale)
							),
							RCTCeilSize(sourceSize, destScale)
						);
					}
			}
		}
	}
}

