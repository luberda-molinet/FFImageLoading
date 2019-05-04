using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Helpers;
using FFImageLoading.Work;

namespace FFImageLoading.Helpers.Gif
{
	public abstract class GifHelperBase<TNativeImageContainer> : IDisposable
	{
		private const int MAX_STACK_SIZE = 4 * 1024;
		private const int NULL_CODE = -1;
		private const int INITIAL_FRAME_POINTER = -1;
		private const int BYTES_PER_INTEGER = 32 / 8;
		private const int MASK_INT_LOWEST_BYTE = 0x000000FF;
		private const int COLOR_TRANSPARENT_BLACK = 0x00000000;
		private const int TOTAL_ITERATION_COUNT_FOREVER = 0;
		private int[] act;
		private int[] pct = new int[256];
		private MemoryStream rawData;
		private byte[] block;

		// LZW decoder working arrays.
		private short[] prefix;
		private byte[] suffix;
		private byte[] pixelStack;
		private byte[] mainPixels;
		private int[] mainScratch;

		private int framePointer;
		private GifHeader header;
		private TNativeImageContainer previousImage;
		private bool savePrevious;
		private GifDecodeStatus status;
		private int sampleSize;
		public int DownsampledHeight { get; private set; }
		public int DownsampledWidth { get; private set; }
		public bool? IsFirstFrameTransparent { get; private set; }

		protected abstract void Release(TNativeImageContainer bitmap);
		protected abstract void SetPixels(TNativeImageContainer bitmap, int[] pixels, int width, int height);
		protected abstract void GetPixels(TNativeImageContainer bitmap, int[] pixels, int width, int height);

		public int Width => header.Width;
		public int Height => header.Height;
		public MemoryStream Data => rawData;
		public GifDecodeStatus Status => status;

		public void Advance()
		{
			framePointer = (framePointer + 1) % header.FrameCount;
		}

		public int GetDelay(int n)
		{
			int delay = -1;
			if ((n >= 0) && (n < header.FrameCount))
			{
				delay = header.Frames[n].Delay;
			}
			return delay;
		}

		public int GetNextDelay()
		{
			if (header.FrameCount <= 0 || framePointer < 0)
			{
				return 0;
			}

			return GetDelay(framePointer);
		}

		public int FrameCount => header.FrameCount;

		public int CurrentFrameIndex => framePointer;

		public void ResetFrameIndex()
		{
			framePointer = INITIAL_FRAME_POINTER;
		}

		public int NetscapeLoopCount => header.LoopCount;

		public int GetTotalIterationCount()
		{
			if (header.LoopCount == (int)GifHeader.LoopCountType.NETSCAPE_LOOP_COUNT_DOES_NOT_EXIST)
			{
				return 1;
			}
			if (header.LoopCount == (int)GifHeader.LoopCountType.NETSCAPE_LOOP_COUNT_FOREVER)
			{
				return TOTAL_ITERATION_COUNT_FOREVER;
			}
			return header.LoopCount + 1;
		}

		public int GetByteSize()
		{
			return (int)rawData.Length + mainPixels.Length + (mainScratch.Length * BYTES_PER_INTEGER);
		}

		public TNativeImageContainer GetNextFrame()
		{
			if (header.FrameCount <= 0 || framePointer < 0)
			{
				status = GifDecodeStatus.STATUS_FORMAT_ERROR;
			}
			if (status == GifDecodeStatus.STATUS_FORMAT_ERROR || status == GifDecodeStatus.STATUS_OPEN_ERROR)
			{
				return default;
			}
			status = GifDecodeStatus.STATUS_OK;

			if (block == null)
			{
				block = new byte[255];
			}

			GifFrame currentFrame = header.Frames[framePointer];
			GifFrame previousFrame = null;
			int previousIndex = framePointer - 1;
			if (previousIndex >= 0)
			{
				previousFrame = header.Frames[previousIndex];
			}

			// Set the appropriate color table.
			act = currentFrame.LCT ?? header.GCT;
			if (act == null)
			{
				// No color table defined.
				status = GifDecodeStatus.STATUS_FORMAT_ERROR;
				return default;
			}

			// Reset the transparent pixel in the color table
			if (currentFrame.Transparency)
			{
				// Prepare local copy of color table ("pct = act"), see #1068
				Array.Copy(act, 0, pct, 0, act.Length);
				// Forget about act reference from shared header object, use copied version
				act = pct;
				// Set transparent color if specified.
				act[currentFrame.TransparencyIndex] = COLOR_TRANSPARENT_BLACK;

				if (currentFrame.Dispose == GifFrame.Disposal.BACKGROUND && framePointer == 0)
				{
					// TODO: We should check and see if all individual pixels are replaced. If they are, the
					// first frame isn't actually transparent. For now, it's simpler and safer to assume
					// drawing a transparent background means the GIF contains transparency.
					IsFirstFrameTransparent = true;
				}
			}

			// Transfer pixel data to image.
			return SetPixels(currentFrame, previousFrame);
		}

		public GifDecodeStatus Read(MemoryStream input, int sampleSize)
		{
			if (input == null)
			{
				status = GifDecodeStatus.STATUS_OPEN_ERROR;
				return status;
			}

			header = new GifHeaderParser(input).ParseHeader();
			if (input != null)
			{
				SetData(header, input, sampleSize);
			}

			return status;
		}

		public void Clear()
		{
			if (previousImage != default)
			{
				Release(previousImage);
			}

			header = null;
			mainPixels = null;
			mainScratch = null;
			block = null;
			previousImage = default;
			rawData = null;
			IsFirstFrameTransparent = null;
		}

		private void SetData(GifHeader header, MemoryStream buffer, int sampleSize)
		{
			if (sampleSize <= 0)
			{
				throw new Exception("Sample size must be >=0, not: " + sampleSize);
			}
			// Make sure sample size is a power of 2.
			sampleSize = sampleSize.HighestOneBit();
			this.status = GifDecodeStatus.STATUS_OK;
			this.header = header;
			framePointer = INITIAL_FRAME_POINTER;
			// Initialize the raw data buffer.
			rawData = buffer;
			rawData.Position = 0;

			// No point in specially saving an old frame if we're never going to use it.
			savePrevious = false;

			foreach (var frame in header.Frames)
			{
				if (frame.Dispose == GifFrame.Disposal.PREVIOUS)
				{
					savePrevious = true;
					break;
				}
			}

			this.sampleSize = sampleSize;
			downsampledWidth = header.Width / sampleSize;
			downsampledHeight = header.Height / sampleSize;
			// Now that we know the size, init scratch arrays.
			// TODO Find a way to avoid this entirely or at least downsample it (either should be possible).
			mainPixels = new byte[header.Width * header.Height];
			mainScratch = new int[downsampledWidth * downsampledHeight];
		}

		private TNativeImageContainer SetPixels(GifFrame currentFrame, GifFrame previousFrame)
		{
			// Final location of blended pixels.
			int[] dest = mainScratch;

			// clear all pixels when meet first frame and drop prev image from last loop
			if (previousFrame == null)
			{
				if (previousImage != default)
				{
					Release(previousImage);
				}
				previousImage = default;
				dest.Fill(COLOR_TRANSPARENT_BLACK);
			}

			// clear all pixels when dispose is 3 but previousImage is null.
			// When DISPOSAL_PREVIOUS and previousImage didn't be set, new frame should draw on
			// a empty image
			if (previousFrame != null && previousFrame.Dispose == GifFrame.Disposal.PREVIOUS
					&& previousImage == default)
			{
				dest.Fill(COLOR_TRANSPARENT_BLACK);
			}

			// fill in starting image contents based on last image's dispose code
			if (previousFrame != null && (int)previousFrame.Dispose > (int)GifFrame.Disposal.UNSPECIFIED)
			{
				// We don't need to do anything for DISPOSAL_NONE, if it has the correct pixels so will our
				// mainScratch and therefore so will our dest array.
				if (previousFrame.Dispose == GifFrame.Disposal.BACKGROUND)
				{
					// Start with a canvas filled with the background color
					int c = COLOR_TRANSPARENT_BLACK;
					if (!currentFrame.Transparency)
					{
						c = header.BackgroundColor;
						if (currentFrame.LCT != null && header.BackgroundIndex == currentFrame.TransparencyIndex)
						{
							c = COLOR_TRANSPARENT_BLACK;
						}
					}
					// The area used by the graphic must be restored to the background color.
					int downsampledIH = previousFrame.Height / sampleSize;
					int downsampledIY = previousFrame.Y / sampleSize;
					int downsampledIW = previousFrame.Width / sampleSize;
					int downsampledIX = previousFrame.X / sampleSize;
					int topLeft = downsampledIY * downsampledWidth + downsampledIX;
					int bottomLeft = topLeft + downsampledIH * downsampledWidth;
					for (int left = topLeft; left < bottomLeft; left += downsampledWidth)
					{
						int right = left + downsampledIW;
						for (int pointer = left; pointer < right; pointer++)
						{
							dest[pointer] = c;
						}
					}
				}
				else if (previousFrame.Dispose == GifFrame.Disposal.PREVIOUS && previousImage != default)
				{
					// Start with the previous frame
					GetPixels(previousImage, dest, downsampledWidth, downsampledHeight);
				}
			}

			// Decode pixels for this frame into the global pixels[] scratch.
			DecodeBitmapData(currentFrame);

			if (currentFrame.Interlace || sampleSize != 1)
			{
				CopyCopyIntoScratchRobust(currentFrame);
			}
			else
			{
				CopyIntoScratchFast(currentFrame);
			}

			// Copy pixels into previous image
			if (savePrevious && (currentFrame.Dispose == GifFrame.Disposal.UNSPECIFIED
				|| currentFrame.Dispose == GifFrame.Disposal.NONE))
			{
				if (previousImage == default)
				{
					previousImage = GetNextBitmap();
				}
				SetPixels(previousImage, dest, downsampledWidth, downsampledHeight);
			}

			// Set pixels for current image.
			var result = GetNextBitmap();
			SetPixels(result, dest, downsampledWidth, downsampledHeight);
			return result;
		}

		private void CopyIntoScratchFast(GifFrame currentFrame)
		{
			int[] dest = mainScratch;
			int downsampledIH = currentFrame.Height;
			int downsampledIY = currentFrame.Y;
			int downsampledIW = currentFrame.Width;
			int downsampledIX = currentFrame.X;
			// Copy each source line to the appropriate place in the destination.
			bool isFirstFrame = framePointer == 0;
			int width = this.downsampledWidth;
			byte[] mainPixels = this.mainPixels;
			int[] act = this.act;
			byte transparentColorIndex = Convert.ToByte(-1);
			for (int i = 0; i < downsampledIH; i++)
			{
				int line = i + downsampledIY;
				int k = line * width;
				// Start of line in dest.
				int dx = k + downsampledIX;
				// End of dest line.
				int dlim = dx + downsampledIW;
				if (k + width < dlim)
				{
					// Past dest edge.
					dlim = k + width;
				}
				// Start of line in source.
				int sx = i * currentFrame.Width;

				while (dx < dlim)
				{
					byte byteCurrentColorIndex = mainPixels[sx];
					int currentColorIndex = ((int)byteCurrentColorIndex) & MASK_INT_LOWEST_BYTE;
					if (currentColorIndex != transparentColorIndex)
					{
						int color = act[currentColorIndex];
						if (color != COLOR_TRANSPARENT_BLACK)
						{
							dest[dx] = color;
						}
						else
						{
							transparentColorIndex = byteCurrentColorIndex;
						}
					}
					++sx;
					++dx;
				}
			}

			IsFirstFrameTransparent = IsFirstFrameTransparent.GetValueOrDefault()
					|| (isFirstFrame && transparentColorIndex != Convert.ToByte(-1));
		}

		private void CopyCopyIntoScratchRobust(GifFrame currentFrame)
		{
			int sampleSize = this.sampleSize;
			var isFirstFrameTransparent = IsFirstFrameTransparent;
			int[] dest = mainScratch;
			int downsampledIH = currentFrame.Height / sampleSize;
			int downsampledIY = currentFrame.Y / sampleSize;
			int downsampledIW = currentFrame.Width / sampleSize;
			int downsampledIX = currentFrame.X / sampleSize;
			// Copy each source line to the appropriate place in the destination.
			int pass = 1;
			int inc = 8;
			int iline = 0;
			var isFirstFrame = framePointer == 0;
			int downsampledWidth = this.downsampledWidth;
			int downsampledHeight = this.downsampledHeight;
			byte[] mainPixels = this.mainPixels;
			int[] act = this.act;


			for (int i = 0; i < downsampledIH; i++)
			{
				int line = i;
				if (currentFrame.Interlace)
				{
					if (iline >= downsampledIH)
					{
						pass++;
						switch (pass)
						{
							case 2:
								iline = 4;
								break;
							case 3:
								iline = 2;
								inc = 4;
								break;
							case 4:
								iline = 1;
								inc = 2;
								break;
							default:
								break;
						}
					}
					line = iline;
					iline += inc;
				}
				line += downsampledIY;
				bool isNotDownsampling = sampleSize == 1;
				if (line < downsampledHeight)
				{
					int k = line * downsampledWidth;
					// Start of line in dest.
					int dx = k + downsampledIX;
					// End of dest line.
					int dlim = dx + downsampledIW;
					if (k + downsampledWidth < dlim)
					{
						// Past dest edge.
						dlim = k + downsampledWidth;
					}
					// Start of line in source.
					int sx = i * sampleSize * currentFrame.Width;
					if (isNotDownsampling)
					{
						int averageColor;
						while (dx < dlim)
						{
							int currentColorIndex = ((int)mainPixels[sx]) & MASK_INT_LOWEST_BYTE;
							averageColor = act[currentColorIndex];
							if (averageColor != COLOR_TRANSPARENT_BLACK)
							{
								dest[dx] = averageColor;
							}
							else if (isFirstFrame && isFirstFrameTransparent == null)
							{
								isFirstFrameTransparent = true;
							}
							sx += sampleSize;
							dx++;
						}
					}
					else
					{
						int averageColor;
						int maxPositionInSource = sx + ((dlim - dx) * sampleSize);
						while (dx < dlim)
						{
							// Map color and insert in destination.
							// TODO: This is substantially slower (up to 50ms per frame) than just grabbing the
							// current color index above, even with a sample size of 1.
							averageColor = AverageColorsNear(sx, maxPositionInSource, currentFrame.Width);
							if (averageColor != COLOR_TRANSPARENT_BLACK)
							{
								dest[dx] = averageColor;
							}
							else if (isFirstFrame && isFirstFrameTransparent == null)
							{
								isFirstFrameTransparent = true;
							}
							sx += sampleSize;
							dx++;
						}
					}
				}
			}

			if (IsFirstFrameTransparent == null)
			{
				IsFirstFrameTransparent = isFirstFrameTransparent == null
					? false : isFirstFrameTransparent;
			}
		}

		private int AverageColorsNear(int positionInMainPixels, int maxPositionInMainPixels, int currentFrameIw)
		{
			int alphaSum = 0;
			int redSum = 0;
			int greenSum = 0;
			int blueSum = 0;

			int totalAdded = 0;
			// Find the pixels in the current row.
			for (int i = positionInMainPixels;
				 i < positionInMainPixels + sampleSize && i < mainPixels.Length
					 && i < maxPositionInMainPixels; i++)
			{
				int currentColorIndex = ((int)mainPixels[i]) & MASK_INT_LOWEST_BYTE;
				int currentColor = act[currentColorIndex];
				if (currentColor != 0)
				{
					alphaSum += currentColor >> 24 & MASK_INT_LOWEST_BYTE;
					redSum += currentColor >> 16 & MASK_INT_LOWEST_BYTE;
					greenSum += currentColor >> 8 & MASK_INT_LOWEST_BYTE;
					blueSum += currentColor & MASK_INT_LOWEST_BYTE;
					totalAdded++;
				}
			}
			// Find the pixels in the next row.
			for (int i = positionInMainPixels + currentFrameIw;
				 i < positionInMainPixels + currentFrameIw + sampleSize && i < mainPixels.Length
					 && i < maxPositionInMainPixels; i++)
			{
				int currentColorIndex = ((int)mainPixels[i]) & MASK_INT_LOWEST_BYTE;
				int currentColor = act[currentColorIndex];
				if (currentColor != 0)
				{
					alphaSum += currentColor >> 24 & MASK_INT_LOWEST_BYTE;
					redSum += currentColor >> 16 & MASK_INT_LOWEST_BYTE;
					greenSum += currentColor >> 8 & MASK_INT_LOWEST_BYTE;
					blueSum += currentColor & MASK_INT_LOWEST_BYTE;
					totalAdded++;
				}
			}
			if (totalAdded == 0)
			{
				return COLOR_TRANSPARENT_BLACK;
			}
			else
			{
				return ((alphaSum / totalAdded) << 24)
					| ((redSum / totalAdded) << 16)
					| ((greenSum / totalAdded) << 8)
					| (blueSum / totalAdded);
			}
		}

		private void DecodeBitmapData(GifFrame frame)
		{
			if (frame != null)
			{
				// Jump to the frame start position.
				rawData.Position = frame.BufferFrameStart;
			}

			int npix = (frame == null) ? header.Width * header.Height : frame.Width * frame.Height;
			int available, clear, codeMask, codeSize, endOfInformation, inCode, oldCode, bits, code, count,
				i, datum, dataSize, first, top, bi, pi;

			if (mainPixels == null || this.mainPixels.Length < npix)
			{
				// Allocate new pixel array.
				mainPixels = new byte[npix];
			}

			if (prefix == null)
			{
				prefix = new short[MAX_STACK_SIZE];
			}

			if (suffix == null)
			{
				suffix = new byte[MAX_STACK_SIZE];
			}

			if (pixelStack == null)
			{
				pixelStack = new byte[MAX_STACK_SIZE + 1];
			}

			// Initialize GIF data stream decoder.
			dataSize = ReadByte();
			clear = 1 << dataSize;
			endOfInformation = clear + 1;
			available = clear + 2;
			oldCode = NULL_CODE;
			codeSize = dataSize + 1;
			codeMask = (1 << codeSize) - 1;

			for (code = 0; code < clear; code++)
			{
				// XXX ArrayIndexOutOfBoundsException.
				prefix[code] = 0;
				suffix[code] = (byte)code;
			}
			byte[] block = this.block;
			// Decode GIF pixel stream.
			i = datum = bits = count = first = top = pi = bi = 0;
			while (i < npix)
			{
				// Read a new data block.
				if (count == 0)
				{
					count = ReadBlock();
					if (count <= 0)
					{
						status = GifDecodeStatus.STATUS_PARTIAL_DECODE;
						break;
					}
					bi = 0;
				}

				datum += (((int)block[bi]) & MASK_INT_LOWEST_BYTE) << bits;
				bits += 8;
				++bi;
				--count;

				while (bits >= codeSize)
				{
					// Get the next code.
					code = datum & codeMask;
					datum >>= codeSize;
					bits -= codeSize;

					// Interpret the code.
					if (code == clear)
					{
						// Reset decoder.
						codeSize = dataSize + 1;
						codeMask = (1 << codeSize) - 1;
						available = clear + 2;
						oldCode = NULL_CODE;
						continue;
					}
					else if (code == endOfInformation)
					{
						break;
					}
					else if (oldCode == NULL_CODE)
					{
						mainPixels[pi] = suffix[code];
						++pi;
						++i;
						oldCode = code;
						first = code;
						continue;
					}

					inCode = code;
					if (code >= available)
					{
						pixelStack[top] = (byte)first;
						++top;
						code = oldCode;
					}

					while (code >= clear)
					{
						pixelStack[top] = suffix[code];
						++top;
						code = prefix[code];
					}
					first = ((int)suffix[code]) & MASK_INT_LOWEST_BYTE;

					mainPixels[pi] = (byte)first;
					++pi;
					++i;

					while (top > 0)
					{
						// Pop a pixel off the pixel stack.
						mainPixels[pi] = pixelStack[--top];
						++pi;
						++i;
					}

					// Add a new string to the string table.
					if (available < MAX_STACK_SIZE)
					{
						prefix[available] = (short)oldCode;
						suffix[available] = (byte)first;
						++available;
						if (((available & codeMask) == 0) && (available < MAX_STACK_SIZE))
						{
							++codeSize;
							codeMask += available;
						}
					}
					oldCode = inCode;
				}
			}

			// Clear missing pixels.
			mainPixels.Fill(pi, npix, (byte)COLOR_TRANSPARENT_BLACK);
		}

		private int ReadByte()
		{
			return rawData.ReadByte() & MASK_INT_LOWEST_BYTE;
		}

		private int ReadBlock()
		{
			var blockSize = ReadByte();
			if (blockSize <= 0)
			{
				return blockSize;
			}
			rawData.Read(block, 0, Math.Min(blockSize, (int)rawData.Length - (int)rawData.Position));
			return blockSize;
		}

		protected abstract TNativeImageContainer GetNextBitmap();

		public void Dispose()
		{
			Clear();
		}
	}
}
