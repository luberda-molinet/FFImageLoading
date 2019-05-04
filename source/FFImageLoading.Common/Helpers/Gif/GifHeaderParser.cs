using System;
using System.IO;
using System.Text;

namespace FFImageLoading.Helpers.Gif
{
	public class GifHeaderParser
	{
		const int MASK_INT_LOWEST_BYTE = 0x000000FF;
		const int MIN_FRAME_DELAY = 2;
		const int LSD_MASK_GCT_FLAG = 0b10000000;
		const int LSD_MASK_GCT_SIZE = 0b00000111;
		const int IMAGE_SEPARATOR = 0x2C;
		const int EXTENSION_INTRODUCER = 0x21;
		const int TRAILER = 0x3B;
		const int LABEL_GRAPHIC_CONTROL_EXTENSION = 0xF9;
		const int LABEL_APPLICATION_EXTENSION = 0xFF;
		const int LABEL_COMMENT_EXTENSION = 0xFE;
		const int LABEL_PLAIN_TEXT_EXTENSION = 0x01;
		const int GCE_MASK_DISPOSAL_METHOD = 0b00011100;
		const int GCE_DISPOSAL_METHOD_SHIFT = 2;
		const int GCE_MASK_TRANSPARENT_COLOR_FLAG = 0b00000001;
		const int DESCRIPTOR_MASK_LCT_FLAG = 0b10000000;
		const int DESCRIPTOR_MASK_INTERLACE_FLAG = 0b01000000;
		const int DESCRIPTOR_MASK_LCT_SIZE = 0b00000111;

		const int DEFAULT_FRAME_DELAY = 10;
		const int MAX_BLOCK_SIZE = 256;
		byte[] block = new byte[MAX_BLOCK_SIZE];
		int blockSize = 0;
		private MemoryStream rawData;
		private BinaryReader rawDataReader;
		private GifHeader header;

		public GifHeaderParser(MemoryStream data)
		{
			Reset();
			data.Position = 0;
			rawData = data;
			rawDataReader = new BinaryReader(rawData);
		}

		public GifHeaderParser(byte[] data)
		{
			if (data != null)
			{
				Reset();
				rawData = new MemoryStream(data);
				rawDataReader = new BinaryReader(rawData);
			}
			else
			{
				rawData = null;
				header.Status = GifDecodeStatus.STATUS_OPEN_ERROR;
			}
		}

		public void Clear()
		{
			rawData = null;
			header = null;
		}

		private void Reset()
		{
			rawData = null;
			Array.Clear(block, 0, block.Length);
			header = new GifHeader();
			blockSize = 0;
		}

		public GifHeader ParseHeader()
		{
			if (rawData == null)
			{
				throw new ArgumentNullException(nameof(rawData));
			}
			if (Err())
			{
				return header;
			}

			ReadHeader();
			if (!Err())
			{
				ReadContents();
				if (header.FrameCount < 0)
				{
					header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
				}
			}

			return header;
		}

		public bool IsAnimated()
		{
			ReadHeader();
			if (!Err())
			{
				ReadContents(2 /* maxFrames */);
			}
			return header.FrameCount > 1;
		}

		private void ReadContents()
		{
			ReadContents(int.MaxValue /* maxFrames */);
		}

		private void ReadContents(int maxFrames)
		{
			// Read GIF file content blocks.
			bool done = false;
			while (!(done || Err() || header.FrameCount > maxFrames))
			{
				int code = Read();
				switch (code)
				{
					case IMAGE_SEPARATOR:
						// The Graphic Control Extension is optional, but will always come first if it exists.
						// If one did exist, there will be a non-null current frame which we should use.
						// However if one did not exist, the current frame will be null
						// and we must create it here. See issue #134.
						if (header.CurrentFrame == null)
						{
							header.CurrentFrame = new GifFrame();
						}
						ReadBitmap();
						break;
					case EXTENSION_INTRODUCER:
						int extensionLabel = Read();
						switch (extensionLabel)
						{
							case LABEL_GRAPHIC_CONTROL_EXTENSION:
								// Start a new frame.
								header.CurrentFrame = new GifFrame();
								readGraphicControlExt();
								break;
							case LABEL_APPLICATION_EXTENSION:
								ReadBlock();
								var app = new StringBuilder();
								for (int i = 0; i < 11; i++)
								{
									app.Append((char)block[i]);
								}
								if (app.ToString().Equals("NETSCAPE2.0"))
								{
									readNetscapeExt();
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
						header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
						break;
				}
			}
		}

		private void readGraphicControlExt()
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
			header.CurrentFrame.Dispose = (GifFrame.Disposal)((packed & GCE_MASK_DISPOSAL_METHOD) >> GCE_DISPOSAL_METHOD_SHIFT);
			if (header.CurrentFrame.Dispose == GifFrame.Disposal.UNSPECIFIED)
			{
				// Elect to keep old image if discretionary.
				header.CurrentFrame.Dispose = GifFrame.Disposal.NONE;
			}
			header.CurrentFrame.Transparency = (packed & GCE_MASK_TRANSPARENT_COLOR_FLAG) != 0;
			// Delay in milliseconds.
			int delayInHundredthsOfASecond = ReadShort();
			// TODO: consider allowing -1 to indicate show forever.
			if (delayInHundredthsOfASecond < MIN_FRAME_DELAY)
			{
				delayInHundredthsOfASecond = DEFAULT_FRAME_DELAY;
			}
			header.CurrentFrame.Delay = delayInHundredthsOfASecond * 10;
			// Transparent color index
			header.CurrentFrame.TransparencyIndex = Read();
			// Block terminator
			Read();
		}

		private void ReadBitmap()
		{
			// (sub)image position & size.
			header.CurrentFrame.X = ReadShort();
			header.CurrentFrame.Y = ReadShort();
			header.CurrentFrame.Width = ReadShort();
			header.CurrentFrame.Height = ReadShort();

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
			header.CurrentFrame.Interlace = (packed & DESCRIPTOR_MASK_INTERLACE_FLAG) != 0;
			if (lctFlag)
			{
				header.CurrentFrame.LCT = ReadColorTable(lctSize);
			}
			else
			{
				// No local color table.
				header.CurrentFrame.LCT = null;
			}

			// Save this as the decoding position pointer.
			header.CurrentFrame.BufferFrameStart = (int)rawData.Position;

			// False decode pixel data to advance buffer.
			skipImageData();

			if (Err())
			{
				return;
			}

			header.FrameCount++;
			// Add image to frame.
			header.Frames.Add(header.CurrentFrame);
		}

		private void readNetscapeExt()
		{
			do
			{
				ReadBlock();
				if (block[0] == 1)
				{
					// Loop count sub-block.
					int b1 = ((int)block[1]) & MASK_INT_LOWEST_BYTE;
					int b2 = ((int)block[2]) & MASK_INT_LOWEST_BYTE;
					header.LoopCount = (b2 << 8) | b1;
				}
			} while ((blockSize > 0) && !Err());
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
				header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
				return;
			}
			ReadLSD();
			if (header.GCTFlag && !Err())
			{
				header.GCT = ReadColorTable(header.GCTSize);
				header.BackgroundColor = header.GCT[header.BackgroundIndex];
			}
		}

		private void ReadLSD()
		{
			// Logical screen size.
			header.Width = ReadShort();
			header.Height = ReadShort();
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
			header.GCTFlag = (packed & LSD_MASK_GCT_FLAG) != 0;
			header.GCTSize = (int)Math.Pow(2, (packed & LSD_MASK_GCT_SIZE) + 1);
			// Background color index.
			header.BackgroundIndex = Read();
			// Pixel aspect ratio
			header.PixelAspect = Read();
		}

		private int[] ReadColorTable(int nColors)
		{
			int nBytes = 3 * nColors;
			int[] tab = null;
			byte[] c = new byte[nBytes];

			try
			{
				rawData.Read(c, 0, nBytes);

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
				header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
			}

			return tab;
		}

		private void skipImageData()
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
				int newPosition = Math.Min((int)rawData.Position + bSize, (int)rawData.Length);
				rawData.Position = newPosition;
			} while (bSize > 0);
		}

		private void ReadBlock()
		{
			blockSize = Read();
			int n = 0;
			if (blockSize > 0)
			{
				int count = 0;
				try
				{
					while (n < blockSize)
					{
						count = blockSize - n;
						rawData.Read(block, n, count);

						n += count;
					}
				}
				catch (Exception)
				{
					header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
				}
			}
		}

		private int Read()
		{
			var currByte = 0;
			try
			{
				currByte = rawDataReader.Read() & MASK_INT_LOWEST_BYTE;
			}
			catch (Exception)
			{
				header.Status = GifDecodeStatus.STATUS_FORMAT_ERROR;
			}
			return currByte;
		}

		private int ReadShort()
		{
			// Read 16-bit value.
			return rawDataReader.ReadInt16();
		}

		private bool Err()
		{
			return header.Status != GifDecodeStatus.STATUS_OK;
		}
	}
}
