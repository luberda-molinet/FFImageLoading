using System;
using System.IO;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers.Gif
{
	public abstract class GifHelperBase<TNativeImageContainer> : IDisposable
	{
#pragma warning disable IDE1006 // Naming Styles
		private const int MAX_STACK_SIZE = 4 * 1024;
		private const int NULL_CODE = -1;
		private const int INITIAL_FRAME_POINTER = -1;
		private const int BYTES_PER_INTEGER = 32 / 8;
		private const int MASK_INT_LOWEST_BYTE = 0x000000FF;
		private const int COLOR_TRANSPARENT_BLACK = 0x00000000;
		private const int TOTAL_ITERATION_COUNT_FOREVER = 0;
#pragma warning restore IDE1006 // Naming Styles
		private int[] _act;
		private readonly int[] _pct = new int[256];
		private byte[] _block;

		// LZW decoder working arrays.
		private short[] _prefix;
		private byte[] _suffix;
		private byte[] _pixelStack;
		private byte[] _mainPixels;
		private int[] _mainScratch;
		private GifHeader _header;
		private TNativeImageContainer _previousImage;
		private bool _savePrevious;
		private int _sampleSize;

		protected abstract void Release(TNativeImageContainer bitmap);
		protected abstract void SetPixels(TNativeImageContainer bitmap, int[] pixels, int width, int height);
		protected abstract void GetPixels(TNativeImageContainer bitmap, int[] pixels, int width, int height);

		public int Width => _header.Width;
		public int Height => _header.Height;
        protected Stream Data { get; private set; }
        public GifDecodeStatus Status { get; private set; }
		public int DownsampledHeight { get; private set; }
		public int DownsampledWidth { get; private set; }
		public bool? IsFirstFrameTransparent { get; private set; }

		public async Task ReadHeaderAsync(Stream input)
		{
			input = await input.AsSeekableStreamAsync().ConfigureAwait(false);
			var parser = new GifHeaderParser(input);
			_header = await parser.ParseHeaderAsync().ConfigureAwait(false);
		}

		public async Task<GifDecodeStatus> ReadAsync(Stream input, int sampleSize)
		{
			if (input == null)
			{
				Status = GifDecodeStatus.STATUS_OPEN_ERROR;
				return Status;
			}

			if (_header == null)
			{
				await ReadHeaderAsync(input).ConfigureAwait(false);
			}

			if (input != null)
			{
				SetData(_header, input, sampleSize);
			}

			return Status;
		}

		public void Advance()
		{
			CurrentFrameIndex = (CurrentFrameIndex + 1) % _header.FrameCount;
		}

		public int GetDelay(int n)
		{
			int delay = -1;
			if ((n >= 0) && (n < _header.FrameCount))
			{
				delay = _header.Frames[n].Delay;
			}
			return delay;
		}

		public int GetNextDelay()
		{
			if (_header.FrameCount <= 0 || CurrentFrameIndex < 0)
			{
				return 0;
			}

			return GetDelay(CurrentFrameIndex);
		}

		public int FrameCount => _header.FrameCount;

        public int CurrentFrameIndex { get; private set; }

        public void ResetFrameIndex()
		{
			CurrentFrameIndex = INITIAL_FRAME_POINTER;
		}

		public int NetscapeLoopCount => _header.LoopCount;

		public int GetTotalIterationCount()
		{
			if (_header.LoopCount == (int)GifHeader.LoopCountType.NETSCAPE_LOOP_COUNT_DOES_NOT_EXIST)
			{
				return 1;
			}
			if (_header.LoopCount == (int)GifHeader.LoopCountType.NETSCAPE_LOOP_COUNT_FOREVER)
			{
				return TOTAL_ITERATION_COUNT_FOREVER;
			}
			return _header.LoopCount + 1;
		}

		public int GetByteSize()
		{
			return (int)Data.Length + _mainPixels.Length + (_mainScratch.Length * BYTES_PER_INTEGER);
		}

		public async Task<TNativeImageContainer> GetNextFrameAsync()
		{
			if (_header.FrameCount <= 0 || CurrentFrameIndex < 0)
			{
				Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
			}
			if (Status == GifDecodeStatus.STATUS_FORMAT_ERROR || Status == GifDecodeStatus.STATUS_OPEN_ERROR)
			{
				return default;
			}
			Status = GifDecodeStatus.STATUS_OK;

			if (_block == null)
			{
				_block = new byte[255];
			}

			GifFrame currentFrame = _header.Frames[CurrentFrameIndex];
			GifFrame previousFrame = null;
			int previousIndex = CurrentFrameIndex - 1;
			if (previousIndex >= 0)
			{
				previousFrame = _header.Frames[previousIndex];
			}

			// Set the appropriate color table.
			_act = currentFrame.LCT ?? _header.GCT;
			if (_act == null)
			{
				// No color table defined.
				Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
				return default;
			}

			// Reset the transparent pixel in the color table
			if (currentFrame.Transparency)
			{
				// Prepare local copy of color table ("pct = act"), see #1068
				Array.Copy(_act, 0, _pct, 0, _act.Length);
				// Forget about act reference from shared header object, use copied version
				_act = _pct;
				// Set transparent color if specified.
				_act[currentFrame.TransparencyIndex] = COLOR_TRANSPARENT_BLACK;

				if (currentFrame.Dispose == GifFrame.Disposal.BACKGROUND && CurrentFrameIndex == 0)
				{
					// TODO: We should check and see if all individual pixels are replaced. If they are, the
					// first frame isn't actually transparent. For now, it's simpler and safer to assume
					// drawing a transparent background means the GIF contains transparency.
					IsFirstFrameTransparent = true;
				}
			}

			// Transfer pixel data to image.
			return await SetPixelsAsync(currentFrame, previousFrame).ConfigureAwait(false);
		}

		public void Clear()
		{
			if (_previousImage != default)
			{
				Release(_previousImage);
			}

			_header = null;
			_mainPixels = null;
			_mainScratch = null;
			_block = null;
			_previousImage = default;
			Data = null;
			IsFirstFrameTransparent = null;
		}

		private void SetData(GifHeader header, Stream buffer, int sampleSize)
		{
			if (sampleSize <= 0)
			{
				throw new Exception("Sample size must be >=0, not: " + sampleSize);
			}
			// Make sure sample size is a power of 2.
			sampleSize = sampleSize.HighestOneBit();
			this.Status = GifDecodeStatus.STATUS_OK;
			this._header = header;
			CurrentFrameIndex = INITIAL_FRAME_POINTER;
			// Initialize the raw data buffer.
			Data = buffer;
			Data.Position = 0;

			// No point in specially saving an old frame if we're never going to use it.
			_savePrevious = false;

			foreach (var frame in header.Frames)
			{
				if (frame.Dispose == GifFrame.Disposal.PREVIOUS)
				{
					_savePrevious = true;
					break;
				}
			}

			this._sampleSize = sampleSize;
			DownsampledWidth = header.Width / sampleSize;
			DownsampledHeight = header.Height / sampleSize;
			// Now that we know the size, init scratch arrays.
			// TODO Find a way to avoid this entirely or at least downsample it (either should be possible).
			_mainPixels = new byte[header.Width * header.Height];
			_mainScratch = new int[DownsampledWidth * DownsampledHeight];
		}

		private async Task<TNativeImageContainer> SetPixelsAsync(GifFrame currentFrame, GifFrame previousFrame)
		{
			// Final location of blended pixels.
			int[] dest = _mainScratch;

			// clear all pixels when meet first frame and drop prev image from last loop
			if (previousFrame == null)
			{
				if (_previousImage != default)
				{
					Release(_previousImage);
				}
				_previousImage = default;
				dest.Fill(COLOR_TRANSPARENT_BLACK);
			}

			// clear all pixels when dispose is 3 but previousImage is null.
			// When DISPOSAL_PREVIOUS and previousImage didn't be set, new frame should draw on
			// a empty image
			if (previousFrame != null && previousFrame.Dispose == GifFrame.Disposal.PREVIOUS
					&& _previousImage == default)
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
						c = _header.BackgroundColor;
						if (currentFrame.LCT != null && _header.BackgroundIndex == currentFrame.TransparencyIndex)
						{
							c = COLOR_TRANSPARENT_BLACK;
						}
					}
					// The area used by the graphic must be restored to the background color.
					int downsampledIH = previousFrame.Height / _sampleSize;
					int downsampledIY = previousFrame.Y / _sampleSize;
					int downsampledIW = previousFrame.Width / _sampleSize;
					int downsampledIX = previousFrame.X / _sampleSize;
					int topLeft = downsampledIY * DownsampledWidth + downsampledIX;
					int bottomLeft = topLeft + downsampledIH * DownsampledWidth;
					for (int left = topLeft; left < bottomLeft; left += DownsampledWidth)
					{
						int right = left + downsampledIW;
						for (int pointer = left; pointer < right; pointer++)
						{
							dest[pointer] = c;
						}
					}
				}
				else if (previousFrame.Dispose == GifFrame.Disposal.PREVIOUS && _previousImage != default)
				{
					// Start with the previous frame
					GetPixels(_previousImage, dest, DownsampledWidth, DownsampledHeight);
				}
			}

			// Decode pixels for this frame into the global pixels[] scratch.
			await DecodeBitmapDataAsync(currentFrame).ConfigureAwait(false);

			if (currentFrame.Interlace || _sampleSize != 1)
			{
				CopyCopyIntoScratchRobust(currentFrame);
			}
			else
			{
				CopyIntoScratchFast(currentFrame);
			}

			// Copy pixels into previous image
			if (_savePrevious && (currentFrame.Dispose == GifFrame.Disposal.UNSPECIFIED
				|| currentFrame.Dispose == GifFrame.Disposal.NONE))
			{
				if (_previousImage == default)
				{
					_previousImage = GetNextBitmap();
				}
				SetPixels(_previousImage, dest, DownsampledWidth, DownsampledHeight);
			}

			// Set pixels for current image.
			var result = GetNextBitmap();
			SetPixels(result, dest, DownsampledWidth, DownsampledHeight);
			return result;
		}

		private void CopyIntoScratchFast(GifFrame currentFrame)
		{
			int[] dest = _mainScratch;
			int downsampledIH = currentFrame.Height;
			int downsampledIY = currentFrame.Y;
			int downsampledIW = currentFrame.Width;
			int downsampledIX = currentFrame.X;
			// Copy each source line to the appropriate place in the destination.
			bool isFirstFrame = CurrentFrameIndex == 0;
			int width = DownsampledWidth;
			byte[] mainPixels = this._mainPixels;
			int[] act = this._act;
			byte? transparentColorIndex = null;
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
					|| (isFirstFrame && transparentColorIndex != null);
		}

		private void CopyCopyIntoScratchRobust(GifFrame currentFrame)
		{
			int sampleSize = this._sampleSize;
			var isFirstFrameTransparent = IsFirstFrameTransparent;
			int[] dest = _mainScratch;
			int downsampledIH = currentFrame.Height / sampleSize;
			int downsampledIY = currentFrame.Y / sampleSize;
			int downsampledIW = currentFrame.Width / sampleSize;
			int downsampledIX = currentFrame.X / sampleSize;
			// Copy each source line to the appropriate place in the destination.
			int pass = 1;
			int inc = 8;
			int iline = 0;
			var isFirstFrame = CurrentFrameIndex == 0;
			int downsampledWidth = DownsampledWidth;
			int downsampledHeight = DownsampledHeight;
			byte[] mainPixels = this._mainPixels;
			int[] act = this._act;


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
				 i < positionInMainPixels + _sampleSize && i < _mainPixels.Length
					 && i < maxPositionInMainPixels; i++)
			{
				int currentColorIndex = ((int)_mainPixels[i]) & MASK_INT_LOWEST_BYTE;
				int currentColor = _act[currentColorIndex];
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
				 i < positionInMainPixels + currentFrameIw + _sampleSize && i < _mainPixels.Length
					 && i < maxPositionInMainPixels; i++)
			{
				int currentColorIndex = ((int)_mainPixels[i]) & MASK_INT_LOWEST_BYTE;
				int currentColor = _act[currentColorIndex];
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

		private async Task DecodeBitmapDataAsync(GifFrame frame)
		{
			if (frame != null)
			{
				// Jump to the frame start position.
				Data.Position = frame.BufferFrameStart;
			}

			int npix = (frame == null) ? _header.Width * _header.Height : frame.Width * frame.Height;
			int available, clear, codeMask, codeSize, endOfInformation, inCode, oldCode, bits, code, count,
				i, datum, dataSize, first, top, bi, pi;

			if (_mainPixels == null || this._mainPixels.Length < npix)
			{
				// Allocate new pixel array.
				_mainPixels = new byte[npix];
			}

			if (_prefix == null)
			{
				_prefix = new short[MAX_STACK_SIZE];
			}

			if (_suffix == null)
			{
				_suffix = new byte[MAX_STACK_SIZE];
			}

			if (_pixelStack == null)
			{
				_pixelStack = new byte[MAX_STACK_SIZE + 1];
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
				_prefix[code] = 0;
				_suffix[code] = (byte)code;
			}
			byte[] block = this._block;
			// Decode GIF pixel stream.
			i = datum = bits = count = first = top = pi = bi = 0;
			while (i < npix)
			{
				// Read a new data block.
				if (count == 0)
				{
					count = await ReadBlockAsync().ConfigureAwait(false);
					if (count <= 0)
					{
						Status = GifDecodeStatus.STATUS_PARTIAL_DECODE;
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
						_mainPixels[pi] = _suffix[code];
						++pi;
						++i;
						oldCode = code;
						first = code;
						continue;
					}

					inCode = code;
					if (code >= available)
					{
						_pixelStack[top] = (byte)first;
						++top;
						code = oldCode;
					}

					while (code >= clear)
					{
						_pixelStack[top] = _suffix[code];
						++top;
						code = _prefix[code];
					}
					first = ((int)_suffix[code]) & MASK_INT_LOWEST_BYTE;

					_mainPixels[pi] = (byte)first;
					++pi;
					++i;

					while (top > 0)
					{
						// Pop a pixel off the pixel stack.
						_mainPixels[pi] = _pixelStack[--top];
						++pi;
						++i;
					}

					// Add a new string to the string table.
					if (available < MAX_STACK_SIZE)
					{
						_prefix[available] = (short)oldCode;
						_suffix[available] = (byte)first;
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
			_mainPixels.Fill(pi, npix, (byte)COLOR_TRANSPARENT_BLACK);
		}

		private int ReadByte()
		{
			return Data.ReadByte() & MASK_INT_LOWEST_BYTE;
		}

		private async Task<int> ReadBlockAsync()
		{
			var blockSize = ReadByte();
			if (blockSize <= 0)
			{
				return blockSize;
			}
			await Data.ReadAsync(_block, 0, Math.Min(blockSize, (int)Data.Length - (int)Data.Position)).ConfigureAwait(false);
			return blockSize;
		}

		protected abstract TNativeImageContainer GetNextBitmap();

		public void Dispose()
		{
			Clear();
		}
	}
}
