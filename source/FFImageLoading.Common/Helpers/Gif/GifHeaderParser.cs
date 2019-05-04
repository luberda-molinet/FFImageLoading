using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers.Gif
{
	public class GifHeaderParser : IDisposable
	{
#pragma warning disable IDE1006 // Naming Styles
		private const int MASK_INT_LOWEST_BYTE = 0x000000FF;
		private const int MIN_FRAME_DELAY = 2;
		private const int LSD_MASK_GCT_FLAG = 0b10000000;
		private const int LSD_MASK_GCT_SIZE = 0b00000111;
		private const int IMAGE_SEPARATOR = 0x2C;
		private const int EXTENSION_INTRODUCER = 0x21;
		private const int TRAILER = 0x3B;
		private const int LABEL_GRAPHIC_CONTROL_EXTENSION = 0xF9;
		private const int LABEL_APPLICATION_EXTENSION = 0xFF;
		private const int LABEL_COMMENT_EXTENSION = 0xFE;
		private const int LABEL_PLAIN_TEXT_EXTENSION = 0x01;
		private const int GCE_MASK_DISPOSAL_METHOD = 0b00011100;
		private const int GCE_DISPOSAL_METHOD_SHIFT = 2;
		private const int GCE_MASK_TRANSPARENT_COLOR_FLAG = 0b00000001;
		private const int DESCRIPTOR_MASK_LCT_FLAG = 0b10000000;
		private const int DESCRIPTOR_MASK_INTERLACE_FLAG = 0b01000000;
		private const int DESCRIPTOR_MASK_LCT_SIZE = 0b00000111;
		private const int DEFAULT_FRAME_DELAY = 10;
		private const int MAX_BLOCK_SIZE = 256;
#pragma warning restore IDE1006 // Naming Styles
		private readonly byte[] _block = new byte[MAX_BLOCK_SIZE];
		private int _blockSize;

		private Stream _rawData;
		private GifHeader _header = new GifHeader();

		public GifHeaderParser(Stream data)
		{
			Array.Clear(_block, 0, _block.Length);
			data.Position = 0;
			_rawData = data;
		}

		public async Task<GifHeader> ParseHeaderAsync()
		{
			if (_rawData == null)
			{
				throw new ArgumentNullException(nameof(_rawData));
			}

			if (Error)
			{
				return _header;
			}

			ReadHeader();
			if (!Error)
			{
				await ReadContentsAsync().ConfigureAwait(false);
				if (_header.FrameCount < 0)
				{
					_header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
				}
			}

			return _header;
		}

		public async Task<bool> IsAnimatedAsync()
		{
			ReadHeader();
			if (!Error)
			{
				await ReadContentsAsync(2 /* maxFrames */).ConfigureAwait(false);
			}
			return _header.FrameCount > 1;
		}

		private async Task ReadContentsAsync()
		{
			await ReadContentsAsync(int.MaxValue /* maxFrames */).ConfigureAwait(false);
		}

		private async Task ReadContentsAsync(int maxFrames)
		{
			// Read GIF file content blocks.
			bool done = false;
			while (!(done || Error || _header.FrameCount > maxFrames))
			{
				int code = Read();
				switch (code)
				{
					case IMAGE_SEPARATOR:
						// The Graphic Control Extension is optional, but will always come first if it exists.
						// If one did exist, there will be a non-null current frame which we should use.
						// However if one did not exist, the current frame will be null
						// and we must create it here. See issue #134.
						if (_header.CurrentFrame == null)
						{
							_header.CurrentFrame = new GifFrame();
						}
						ReadBitmap();
						break;
					case EXTENSION_INTRODUCER:
						int extensionLabel = Read();
						switch (extensionLabel)
						{
							case LABEL_GRAPHIC_CONTROL_EXTENSION:
								// Start a new frame.
								_header.CurrentFrame = new GifFrame();
								ReadGraphicControlExt();
								break;
							case LABEL_APPLICATION_EXTENSION:
								await ReadBlockAsync().ConfigureAwait(false);
								var app = new StringBuilder();
								for (int i = 0; i < 11; i++)
								{
									app.Append((char)_block[i]);
								}
								if (app.ToString().Equals("NETSCAPE2.0"))
								{
									await ReadNetscapeExtAsync().ConfigureAwait(false);
								}
								else
								{
									// Don't care.
									Skip();
								}
								break;
							case LABEL_COMMENT_EXTENSION:
								Skip();
								break;
							case LABEL_PLAIN_TEXT_EXTENSION:
								Skip();
								break;
							default:
								// Uninteresting extension.
								Skip();
								break;
						}
						break;
					case TRAILER:
						// This block is a single-field block indicating the end of the GIF Data Stream.
						done = true;
						break;
					// Bad byte, but keep going and see what happens
					case 0x00:
					default:
						_header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
						break;
				}
			}
		}

		private void ReadGraphicControlExt()
		{
			// Block size.
			Read();
			/*
			 * Graphic Control Extension packed field:
			 *      7 6 5 4 3 2 1 0
			 *     +---------------+
			 *  1  |     |     | | |
			 *
			 * Reserved                    3 Bits
			 * Disposal Method             3 Bits
			 * User Input Flag             1 Bit
			 * Transparent Color Flag      1 Bit
			 */
			int packed = Read();
			// Disposal method.
			//noinspection WrongConstant field has to be extracted from packed value
			_header.CurrentFrame.Dispose = (GifFrame.Disposal)((packed & GCE_MASK_DISPOSAL_METHOD) >> GCE_DISPOSAL_METHOD_SHIFT);
			if (_header.CurrentFrame.Dispose == GifFrame.Disposal.UNSPECIFIED)
			{
				// Elect to keep old image if discretionary.
				_header.CurrentFrame.Dispose = GifFrame.Disposal.NONE;
			}
			_header.CurrentFrame.Transparency = (packed & GCE_MASK_TRANSPARENT_COLOR_FLAG) != 0;
			// Delay in milliseconds.
			int delayInHundredthsOfASecond = ReadShort();
			// TODO: consider allowing -1 to indicate show forever.
			if (delayInHundredthsOfASecond < MIN_FRAME_DELAY)
			{
				delayInHundredthsOfASecond = DEFAULT_FRAME_DELAY;
			}
			_header.CurrentFrame.Delay = GifHelper.GetValidFrameDelay(delayInHundredthsOfASecond * 10);
			// Transparent color index
			_header.CurrentFrame.TransparencyIndex = Read();
			// Block terminator
			Read();
		}

		private void ReadBitmap()
		{
			// (sub)image position & size.
			_header.CurrentFrame.X = ReadShort();
			_header.CurrentFrame.Y = ReadShort();
			_header.CurrentFrame.Width = ReadShort();
			_header.CurrentFrame.Height = ReadShort();

			/*
			 * Image Descriptor packed field:
			 *     7 6 5 4 3 2 1 0
			 *    +---------------+
			 * 9  | | | |   |     |
			 *
			 * Local Color Table Flag     1 Bit
			 * Interlace Flag             1 Bit
			 * Sort Flag                  1 Bit
			 * Reserved                   2 Bits
			 * Size of Local Color Table  3 Bits
			 */
			int packed = Read();
			bool lctFlag = (packed & DESCRIPTOR_MASK_LCT_FLAG) != 0;
			int lctSize = (int)Math.Pow(2, (packed & DESCRIPTOR_MASK_LCT_SIZE) + 1);
			_header.CurrentFrame.Interlace = (packed & DESCRIPTOR_MASK_INTERLACE_FLAG) != 0;
			if (lctFlag)
			{
				_header.CurrentFrame.LCT = ReadColorTable(lctSize);
			}
			else
			{
				// No local color table.
				_header.CurrentFrame.LCT = null;
			}

			// Save this as the decoding position pointer.
			_header.CurrentFrame.BufferFrameStart = (int)_rawData.Position;

			// False decode pixel data to advance buffer.
			SkipImageData();

			if (Error)
			{
				return;
			}

			_header.FrameCount++;
			// Add image to frame.
			_header.Frames.Add(_header.CurrentFrame);
		}

		private async Task ReadNetscapeExtAsync()
		{
			do
			{
				await ReadBlockAsync().ConfigureAwait(false);
				if (_block[0] == 1)
				{
					// Loop count sub-block.
					int b1 = ((int)_block[1]) & MASK_INT_LOWEST_BYTE;
					int b2 = ((int)_block[2]) & MASK_INT_LOWEST_BYTE;
					_header.LoopCount = (b2 << 8) | b1;
				}
			} while ((_blockSize > 0) && !Error);
		}

		private void ReadHeader()
		{
			var id = new StringBuilder();
			for (int i = 0; i < 6; i++)
			{
				id.Append((char)Read());
			}
			if (!id.ToString().StartsWith("GIF"))
			{
				_header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
				return;
			}
			ReadLSD();
			if (_header.GCTFlag && !Error)
			{
				_header.GCT = ReadColorTable(_header.GCTSize);
				_header.BackgroundColor = _header.GCT[_header.BackgroundIndex];
			}
		}

		private void ReadLSD()
		{
			// Logical screen size.
			_header.Width = ReadShort();
			_header.Height = ReadShort();
			/*
			 * Logical Screen Descriptor packed field:
			 *      7 6 5 4 3 2 1 0
			 *     +---------------+
			 *  4  | |     | |     |
			 *
			 * Global Color Table Flag     1 Bit
			 * Color Resolution            3 Bits
			 * Sort Flag                   1 Bit
			 * Size of Global Color Table  3 Bits
			 */
			int packed = Read();
			_header.GCTFlag = (packed & LSD_MASK_GCT_FLAG) != 0;
			_header.GCTSize = (int)Math.Pow(2, (packed & LSD_MASK_GCT_SIZE) + 1);
			// Background color index.
			_header.BackgroundIndex = Read();
			// Pixel aspect ratio
			_header.PixelAspect = Read();
		}

		private int[] ReadColorTable(int nColors)
		{
			int nBytes = 3 * nColors;
			int[] tab = null;
			byte[] c = new byte[nBytes];

			try
			{
				_rawData.Read(c, 0, nBytes);

				// TODO: what bounds checks are we avoiding if we know the number of colors?
				// Max size to avoid bounds checks.
				tab = new int[MAX_BLOCK_SIZE];
				int i = 0;
				int j = 0;
				while (i < nColors)
				{
					int r = ((int)c[j++]) & MASK_INT_LOWEST_BYTE;
					int g = ((int)c[j++]) & MASK_INT_LOWEST_BYTE;
					int b = ((int)c[j++]) & MASK_INT_LOWEST_BYTE;
					tab[i++] = (int)(0xFF000000 | (r << 16) | (g << 8) | b);
				}
			}
			catch (Exception)
			{
				_header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
			}

			return tab;
		}

		private void SkipImageData()
		{
			// lzwMinCodeSize
			Read();
			// data sub-blocks
			Skip();
		}

		private void Skip()
		{
			int bSize;
			do
			{
				bSize = Read();
				int newPosition = Math.Min((int)_rawData.Position + bSize, (int)_rawData.Length);
				_rawData.Position = newPosition;
			} while (bSize > 0);
		}

		private async Task ReadBlockAsync()
		{
			_blockSize = Read();
			int n = 0;
			if (_blockSize > 0)
			{
				int count = 0;
				try
				{
					while (n < _blockSize)
					{
						count = _blockSize - n;
						await _rawData.ReadAsync(_block, n, count).ConfigureAwait(false);

						n += count;
					}
				}
				catch (Exception)
				{
					_header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
				}
			}
		}

		private int Read()
		{
			var currByte = 0;
			try
			{
				currByte = _rawData.ReadByte() & MASK_INT_LOWEST_BYTE;
			}
			catch (Exception)
			{
				_header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
			}
			return currByte;
		}

		private short ReadShort()
		{
			// Read 16-bit value.
			return (short)(_rawData.ReadByte() | (_rawData.ReadByte() << 8));
		}

		private bool Error => _header.Status != GifDecodeStatus.STATUS_OK;

		public void Dispose()
		{
			_rawData = null;
		}
	}
}
