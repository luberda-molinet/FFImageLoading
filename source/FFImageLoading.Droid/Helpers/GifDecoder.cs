using System;
using System.Collections.Generic;
using Android.Graphics;

/* Translated to C# from Java class https://code.google.com/archive/p/android-gifview/source/default/source/ */
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace FFImageLoading.Helpers
{
    public class GifDecoder
    {
        object _lock = new object();
        /**
         * File read status: No errors.
         */
        const int STATUS_OK = 0;
        /**
         * File read status: Error decoding file (may be partially decoded)
         */
        const int STATUS_FORMAT_ERROR = 1;
        /**
         * File read status: Unable to open source.
         */
        const int STATUS_OPEN_ERROR = 2;
        /** max decoder pixel stack size */
        const int MAX_STACK_SIZE = 4096;
        Stream input;
        int status;
        int width; // full image width
        int height; // full image height
        bool gctFlag; // global color table used
        int gctSize; // size of global color table
        int loopCount = 1; // iterations; 0 = repeat forever
        int[] gct; // global color table
        int[] lct; // local color table
        int[] act; // active color table
        int bgIndex; // background color index
        int bgColor; // background color
        int lastBgColor; // previous bg color
        int pixelAspect; // pixel aspect ratio
        bool lctFlag; // local color table flag
        bool interlace; // interlace flag
        int lctSize; // local color table size
        int ix, iy, iw, ih; // current image rectangle
        int lrx, lry, lrw, lrh;
        Bitmap image; // current frame
        Bitmap lastBitmap; // previous frame
        byte[] block = new byte[256]; // current data block
        int blockSize = 0; // block size last graphic control extension info
        int dispose = 0; // 0=no action; 1=leave in place; 2=restore to bg; 3=restore to prev
        int lastDispose = 0;
        bool transparency = false; // use transparent color
        int delay = 0; // delay in milliseconds
        int transIndex; // transparent color index
                                  // LZW decoder working arrays
        short[] prefix;
        byte[] suffix;
        byte[] pixelStack;
        byte[] pixels;
        IList<GifFrame> frames; // frames read from current file
        int frameCount;

        public GifDecoder()
        {
            status = STATUS_OK;
            frameCount = 0;
            frames = new List<GifFrame>();
            gct = null;
            lct = null;
        }

        /**
         * Gets display duration for specified frame.
         * 
         * @param n
         *          int index of frame
         * @return delay in milliseconds
         */
        public int GetDelay(int n)
        {
            lock (_lock)
            {
                delay = -1;
                if ((n >= 0) && (n < frameCount))
                {
                    delay = frames[n].Delay;
                }
                return delay;
            }
        }

        /**
         * Gets the number of frames read from file.
         * 
         * @return frame count
         */
        public int GetFrameCount()
        {
            lock (_lock)
            {
                return frameCount;
            }
        }

        /**
         * Gets the first (or only) image read.
         * 
         * @return BufferedBitmap containing first frame, or null if none.
         */
        public Bitmap GetBitmap()
        {
            lock (_lock)
            {
                return GetFrame(0);
            }
        }

        /**
         * Gets the "Netscape" iteration count, if any. A count of 0 means repeat indefinitiely.
         * 
         * @return iteration count if one was specified, else 1.
         */
        public int GetLoopCount()
        {
            lock (_lock)
            {
                return loopCount;
            }
        }

        /**
         * Creates new frame image from current data (and previous frames as specified by their disposition codes).
         */
        protected void SetPixels()
        {
            // expose destination image's pixels as int array
            int[] dest = new int[width * height];
            // fill in starting image contents based on last image's dispose code
            if (lastDispose > 0)
            {
                if (lastDispose == 3)
                {
                    // use image before last
                    int n = frameCount - 2;
                    if (n > 0)
                    {
                        lastBitmap = GetFrame(n - 1);
                    }
                    else
                    {
                        lastBitmap = null;
                    }
                }
                if (lastBitmap != null)
                {
                    lastBitmap.GetPixels(dest, 0, width, 0, 0, width, height);
                    // copy pixels
                    if (lastDispose == 2)
                    {
                        // fill last image rect area with background color
                        int c = 0;
                        if (!transparency)
                        {
                            c = lastBgColor;
                        }
                        for (int i = 0; i < lrh; i++)
                        {
                            int n1 = (lry + i) * width + lrx;
                            int n2 = n1 + lrw;
                            for (int k = n1; k < n2; k++)
                            {
                                dest[k] = c;
                            }
                        }
                    }
                }
            }
            // copy each source line to the appropriate place in the destination
            int pass = 1;
            int inc = 8;
            int iline = 0;
            for (int i = 0; i < ih; i++)
            {
                int line = i;
                if (interlace)
                {
                    if (iline >= ih)
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
                line += iy;
                if (line < height)
                {
                    int k = line * width;
                    int dx = k + ix; // start of line in dest
                    int dlim = dx + iw; // end of dest line
                    if ((k + width) < dlim)
                    {
                        dlim = k + width; // past dest edge
                    }
                    int sx = i * iw; // start of line in source
                    while (dx < dlim)
                    {
                        // map color and insert in destination
                        int index = ((int)pixels[sx++]) & 0xff;
                        int c = act[index];
                        if (c != 0)
                        {
                            dest[dx] = c;
                        }
                        dx++;
                    }
                }
            }
            image = Bitmap.CreateBitmap(dest, width, height, Bitmap.Config.Argb4444);
        }

        /**
         * Reads GIF image from stream
         * 
         * @param is
         *          containing GIF file.
         * @return read status code (0 = no errors)
         */
        public async Task<int> ReadGifAsync(Stream inputStream)
        {
            if (inputStream != null)
            {
                input = inputStream;
                await ReadHeaderAsync();
                if (!Err())
                {
                    await ReadContentsAsync();
                    if (frameCount < 0)
                    {
                        status = STATUS_FORMAT_ERROR;
                    }
                }
            }
            else
            {
                status = STATUS_OPEN_ERROR;
            }

            return status;
        }

        /**
         * Decodes LZW image data into pixel array. Adapted from John Cristy's BitmapMagick.
         */
        protected async Task DecodeBitmapDataAsync()
        {
            int nullCode = -1;
            int npix = iw * ih;
            int available, clear, code_mask, code_size, end_of_information, in_code, old_code, bits, code, count, i, datum, data_size, first, top, bi, pi;
            if ((pixels == null) || (pixels.Length < npix))
            {
                pixels = new byte[npix]; // allocate new pixel array
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
            data_size = Read();
            clear = 1 << data_size;
            end_of_information = clear + 1;
            available = clear + 2;
            old_code = nullCode;
            code_size = data_size + 1;
            code_mask = (1 << code_size) - 1;
            for (code = 0; code < clear; code++)
            {
                prefix[code] = 0; // XXX ArrayIndexOutOfBoundsException
                suffix[code] = (byte)code;
            }
            // Decode GIF pixel stream.
            datum = bits = count = first = top = pi = bi = 0;
            for (i = 0; i < npix;)
            {
                if (top == 0)
                {
                    if (bits < code_size)
                    {
                        // Load bytes until there are enough bits for a code.
                        if (count == 0)
                        {
                            // Read a new data block.
                            count = await ReadBlockAsync();
                            if (count <= 0)
                            {
                                break;
                            }
                            bi = 0;
                        }
                        datum += (((int)block[bi]) & 0xff) << bits;
                        bits += 8;
                        bi++;
                        count--;
                        continue;
                    }
                    // Get the next code.
                    code = datum & code_mask;
                    datum >>= code_size;
                    bits -= code_size;
                    // Interpret the code
                    if ((code > available) || (code == end_of_information))
                    {
                        break;
                    }
                    if (code == clear)
                    {
                        // Reset decoder.
                        code_size = data_size + 1;
                        code_mask = (1 << code_size) - 1;
                        available = clear + 2;
                        old_code = nullCode;
                        continue;
                    }
                    if (old_code == nullCode)
                    {
                        pixelStack[top++] = suffix[code];
                        old_code = code;
                        first = code;
                        continue;
                    }
                    in_code = code;
                    if (code == available)
                    {
                        pixelStack[top++] = (byte)first;
                        code = old_code;
                    }
                    while (code > clear)
                    {
                        pixelStack[top++] = suffix[code];
                        code = prefix[code];
                    }
                    first = ((int)suffix[code]) & 0xff;
                    // Add a new string to the string table,
                    if (available >= MAX_STACK_SIZE)
                    {
                        break;
                    }
                    pixelStack[top++] = (byte)first;
                    prefix[available] = (short)old_code;
                    suffix[available] = (byte)first;
                    available++;
                    if (((available & code_mask) == 0) && (available < MAX_STACK_SIZE))
                    {
                        code_size++;
                        code_mask += available;
                    }
                    old_code = in_code;
                }
                // Pop a pixel off the pixel stack.
                top--;
                pixels[pi++] = pixelStack[top];
                i++;
            }
            for (i = pi; i < npix; i++)
            {
                pixels[i] = 0; // clear missing pixels
            }
        }

        /**
         * Gets the image contents of frame n.
         * 
         * @return BufferedBitmap representation of frame, or null if n is invalid.
         */
        public Bitmap GetFrame(int n)
        {
            lock (_lock)
            {
                if (frameCount <= 0)
                    return null;
                n = n % frameCount;
                return frames[n].Image;
            }
        }

        public bool ContainsBitmap(Bitmap bitmap)
        {
            lock (_lock)
            {
                return frames.Any(v => v.Image == bitmap);
            }
        }

        /**
         * Returns true if an error was encountered during reading/decoding
         */
        protected bool Err()
        {
            return status != STATUS_OK;
        }

        /**
         * Reads a single byte from the input stream.
         */
        protected int Read()
        {
            int curByte = 0;
            try
            {
                curByte = input.ReadByte();
            }
            catch (Exception e)
            {
                status = STATUS_FORMAT_ERROR;
            }
            return curByte;
        }

        /**
         * Reads next variable length block from input.
         * 
         * @return number of bytes stored in "buffer"
         */
        protected async Task<int> ReadBlockAsync()
        {
            blockSize = Read();
            int n = 0;
            if (blockSize > 0)
            {
                try
                {
                    int count = 0;
                    while (n < blockSize)
                    {
                        count = await input.ReadAsync(block, n, blockSize - n);
                        if (count == -1)
                        {
                            break;
                        }
                        n += count;
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }
                if (n < blockSize)
                {
                    status = STATUS_FORMAT_ERROR;
                }
            }
            return n;
        }

        /**
         * Reads color table as 256 RGB integer values
         * 
         * @param ncolors
         *          int number of colors to read
         * @return int array containing 256 colors (packed ARGB with full alpha)
         */
        protected async Task<int[]> ReadColorTableAsync(int ncolors)
        {
            int nbytes = 3 * ncolors;
            int[] tab = null;
            byte[] c = new byte[nbytes];
            int n = 0;
            try
            {
                n = await input.ReadAsync(c, 0, c.Length);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            if (n < nbytes)
            {
                status = STATUS_FORMAT_ERROR;
            }
            else
            {
                tab = new int[256]; // max size to avoid bounds checks
                int i = 0;
                int j = 0;
                while (i < ncolors)
                {
                    int r = ((int)c[j++]) & 0xff;
                    int g = ((int)c[j++]) & 0xff;
                    int b = ((int)c[j++]) & 0xff;
                    tab[i++] = (int)(0xff000000 | (r << 16) | (g << 8) | b);
                }
            }
            return tab;
        }

        /**
         * Main file parser. Reads GIF content blocks.
         */
        protected async Task ReadContentsAsync()
        {
            // read GIF file content blocks
            bool done = false;
            while (!(done || Err()))
            {
                int code = Read();
                switch (code)
                {
                    case 0x2C: // image separator
                        await ReadBitmapAsync();
                        break;
                    case 0x21: // extension
                        code = Read();
                        switch (code)
                        {
                            case 0xf9: // graphics control extension
                                ReadGraphicControlExt();
                                break;
                            case 0xff: // application extension
                                await ReadBlockAsync();
                                String app = "";
                                for (int i = 0; i < 11; i++)
                                {
                                    app += (char)block[i];
                                }
                                if (app.Equals("NETSCAPE2.0", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    await ReadNetscapeExtAsync();
                                }
                                else
                                {
                                    await SkipAsync(); // don't care
                                }
                                break;
                            case 0xfe:// comment extension
                                await SkipAsync();
                                break;
                            case 0x01:// plain text extension
                                await SkipAsync();
                                break;
                            default: // uninteresting extension
                                await SkipAsync();
                                break;
                        }
                        break;
                    case 0x3b: // terminator
                        done = true;
                        break;
                    case 0x00: // bad byte, but keep going and see what happens break;
                    default:
                        status = STATUS_FORMAT_ERROR;
                        break;
                }
            }
        }

        /**
         * Reads Graphics Control Extension values
         */
        protected void ReadGraphicControlExt()
        {
            Read(); // block size
            int packed = Read(); // packed fields
            dispose = (packed & 0x1c) >> 2; // disposal method
            if (dispose == 0)
            {
                dispose = 1; // elect to keep old image if discretionary
            }
            transparency = (packed & 1) != 0;
            delay = ReadShort() * 10; // delay in milliseconds
            transIndex = Read(); // transparent color index
            Read(); // block terminator
        }

        /**
         * Reads GIF file header information.
         */
        protected async Task ReadHeaderAsync()
        {
            String id = "";
            for (int i = 0; i < 6; i++)
            {
                id += (char)Read();
            }
            if (!id.StartsWith("GIF", StringComparison.InvariantCultureIgnoreCase))
            {
                status = STATUS_FORMAT_ERROR;
                return;
            }
            ReadLSD();
            if (gctFlag && !Err())
            {
                gct = await ReadColorTableAsync(gctSize);
                bgColor = gct[bgIndex];
            }
        }

        /**
         * Reads next frame image
         */
        protected async Task ReadBitmapAsync()
        {
            ix = ReadShort(); // (sub)image position & size
            iy = ReadShort();
            iw = ReadShort();
            ih = ReadShort();
            int packed = Read();
            lctFlag = (packed & 0x80) != 0; // 1 - local color table flag interlace
            lctSize = (int)Math.Pow(2, (packed & 0x07) + 1);
            // 3 - sort flag
            // 4-5 - reserved lctSize = 2 << (packed & 7); // 6-8 - local color
            // table size
            interlace = (packed & 0x40) != 0;
            if (lctFlag)
            {
                lct = await ReadColorTableAsync(lctSize); // read table
                act = lct; // make local table active
            }
            else
            {
                act = gct; // make global table active
                if (bgIndex == transIndex)
                {
                    bgColor = 0;
                }
            }
            int save = 0;
            if (transparency)
            {
                save = act[transIndex];
                act[transIndex] = 0; // set transparent color if specified
            }
            if (act == null)
            {
                status = STATUS_FORMAT_ERROR; // no color table defined
            }
            if (Err())
            {
                return;
            }
            await DecodeBitmapDataAsync(); // decode pixel data
            await SkipAsync();
            if (Err())
            {
                return;
            }
            frameCount++;
            // create new image to receive frame data
            image = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb4444);
            SetPixels(); // transfer pixel data to image
            frames.Add(new GifFrame(image, delay)); // add image to frame
                                                           // list
            if (transparency)
            {
                act[transIndex] = save;
            }
            ResetFrame();
        }

        /**
         * Reads Logical Screen Descriptor
         */
        protected void ReadLSD()
        {
            // logical screen size
            width = ReadShort();
            height = ReadShort();
            // packed fields
            int packed = Read();
            gctFlag = (packed & 0x80) != 0; // 1 : global color table flag
                                            // 2-4 : color resolution
                                            // 5 : gct sort flag
            gctSize = 2 << (packed & 7); // 6-8 : gct size
            bgIndex = Read(); // background color index
            pixelAspect = Read(); // pixel aspect ratio
        }

        /**
         * Reads Netscape extenstion to obtain iteration count
         */
        protected async Task ReadNetscapeExtAsync()
        {
            do
            {
                await ReadBlockAsync();
                if (block[0] == 1)
                {
                    // loop count sub-block
                    int b1 = ((int)block[1]) & 0xff;
                    int b2 = ((int)block[2]) & 0xff;
                    loopCount = (b2 << 8) | b1;
                }
            } while ((blockSize > 0) && !Err());
        }

        /**
         * Reads next 16-bit value, LSB first
         */
        protected int ReadShort()
        {
            // read 16-bit value, LSB first
            return Read() | (Read() << 8);
        }

        /**
         * Resets frame state for reading next image.
         */
        protected void ResetFrame()
        {
            lastDispose = dispose;
            lrx = ix;
            lry = iy;
            lrw = iw;
            lrh = ih;
            lastBitmap = image;
            lastBgColor = bgColor;
            dispose = 0;
            transparency = false;
            delay = 0;
            lct = null;
        }

        /**
         * Skips variable length blocks up to and including next zero length block.
         */
        protected async Task SkipAsync()
        {
            do
            {
                await ReadBlockAsync();
            } while ((blockSize > 0) && !Err());
        }

        class GifFrame
        {
            public GifFrame(Bitmap im, int del)
            {
                Image = im;
                Delay = del;
            }

            public Bitmap Image;
            public int Delay;
        }

        public static bool CheckIfAnimated(Stream st)
        {
            byte[] byteCode1 = { 0x00, 0x21, 0xF9, 0x04 };
            byte[] byteCode2 = { 0x00, 0x2C };
            string strTemp;
            byte[] byteContents;
            int iCount;
            int iPos = 0;
            int iPos1;
            int iPos2;

            byteContents = new byte[st.Length];
            st.Read(byteContents, 0, (int)st.Length);
            strTemp = System.Text.Encoding.ASCII.GetString(byteContents);
            byteContents = null;
            iCount = 0;
            while (iCount < 2)
            {
                iPos1 = strTemp.IndexOf(System.Text.Encoding.ASCII.GetString(byteCode1), iPos, StringComparison.Ordinal);
                if (iPos1 == -1) break;
                iPos = iPos1 + 1;
                iPos2 = strTemp.IndexOf(System.Text.Encoding.ASCII.GetString(byteCode2), iPos, StringComparison.Ordinal);
                if (iPos2 == -1) break;
                if ((iPos1 + 8) == iPos2)
                    iCount++;
                iPos = iPos2 + 1;
            }

            st.Position = 0;

            if (iCount > 1) return true;

            return false;
        }
    }
}
