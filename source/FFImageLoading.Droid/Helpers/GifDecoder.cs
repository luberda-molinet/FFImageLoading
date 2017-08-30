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
        const int STATUS_OK = 0;
        const int STATUS_FORMAT_ERROR = 1;
        const int STATUS_OPEN_ERROR = 2;
        const int MAX_STACK_SIZE = 4096;
        Stream input;
        int status;
        int width; 
        int height; 
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
        int[] currentBitmap;
        int[] lastBitmap1; // previous frame
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
        readonly Func<Bitmap, Task<Bitmap>> _decodingFunc;
        readonly int _downsampleWidth;
        readonly int _downsampleHeight;

        public GifDecoder(int downsampleWidth, int downsampleHeight, Func<Bitmap, Task<Bitmap>> decodingFunc)
        {
            _downsampleHeight = downsampleHeight;
            _downsampleWidth = downsampleWidth;
            _decodingFunc = decodingFunc;
            status = STATUS_OK;
            frameCount = 0;
            frames = new List<GifFrame>();
            gct = null;
            lct = null;
        }

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

        public int GetFrameCount()
        {
            lock (_lock)
            {
                return frameCount;
            }
        }

        public Bitmap GetBitmap()
        {
            lock (_lock)
            {
                return GetFrame(0);
            }
        }

        public int GetLoopCount()
        {
            lock (_lock)
            {
                return loopCount;
            }
        }

        protected async Task SetPixelsAsync()
        {
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
                        lastBitmap1 = currentBitmap;
                    }
                    else
                    {
                        lastBitmap1 = null;
                    }
                }
                if (currentBitmap != null)
                {
                    dest = lastBitmap1;

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

            bool downsample = false;
            int downsampleWidth = width;
            int downsampleHeight = height;

            if (_downsampleWidth == 0 && _downsampleHeight != 0)
            {
                downsample = true;
                downsampleWidth = (int)(((float)_downsampleHeight / height) * width);
                downsampleHeight = _downsampleHeight;
            }
            else if (_downsampleHeight == 0 && _downsampleWidth != 0)
            {
                downsample = true;
                downsampleWidth = _downsampleWidth;
                downsampleHeight = (int)(((float)_downsampleWidth / width) * height);
            }

            var result = dest;

            //TODO fix downsampling issue (part of image is cut)
            downsample = false;
            if (downsample)
            {
                //if (downsampleWidth % 2 > 0)
                //{
                //    downsampleWidth--;
                //}
                //if (downsampleHeight % 2 > 0)
                //{
                //    downsampleHeight--;
                //}

                //double inSampleSize = 1D;

                //if (height > downsampleHeight || width > downsampleWidth)
                //{
                //    int halfHeight = (int)(height / 2);
                //    int halfWidth = (int)(width / 2);

                //    // Calculate a inSampleSize that is a power of 2 - the decoder will use a value that is a power of two anyway.
                //    while ((halfHeight / inSampleSize) > downsampleHeight && (halfWidth / inSampleSize) > downsampleWidth)
                //    {
                //        inSampleSize *= 2;
                //    }
                //}

                //var insample = (int)inSampleSize;
                //if (insample > 1)
                //{
                //    downsample = true;

                //    int idh = 0;
                //    int idw = 0;
                //    result = new int[downsampleWidth * downsampleHeight];

                //    for (int h = 0; h < downsampleHeight; h++)
                //    {
                //        idh = insample * h;

                //        for (int w = 0; w < downsampleWidth; w++)
                //        {
                //            idw = insample * w;
                //            var destIdx = idh * width + idw;

                //            if (destIdx < dest.Length)
                //                result[h * downsampleWidth + w] = dest[destIdx];
                //        }
                //    }
                //}
                //else
                //{
                //    downsample = false;
                //}
            }

            currentBitmap = dest;
            var bitmap = Bitmap.CreateBitmap(result, downsample ? downsampleWidth : width, downsample ? downsampleHeight : height, Bitmap.Config.Argb4444);
            image = await _decodingFunc.Invoke(bitmap).ConfigureAwait(false);
        }

        public async Task<int> ReadGifAsync(Stream inputStream)
        {
            if (inputStream != null)
            {
                input = inputStream;
                await ReadHeaderAsync().ConfigureAwait(false);
                if (!Err())
                {
                    await ReadContentsAsync().ConfigureAwait(false);
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
                            count = await ReadBlockAsync().ConfigureAwait(false);
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

        protected bool Err()
        {
            return status != STATUS_OK;
        }

        protected int Read()
        {
            int curByte = 0;
            try
            {
                curByte = input.ReadByte();
            }
            catch (Exception)
            {
                status = STATUS_FORMAT_ERROR;
            }
            return curByte;
        }

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
                        await ReadBitmapAsync().ConfigureAwait(false);
                        break;
                    case 0x21: // extension
                        code = Read();
                        switch (code)
                        {
                            case 0xf9: // graphics control extension
                                ReadGraphicControlExt();
                                break;
                            case 0xff: // application extension
                                await ReadBlockAsync().ConfigureAwait(false);
                                String app = "";
                                for (int i = 0; i < 11; i++)
                                {
                                    app += (char)block[i];
                                }
                                if (app.Equals("NETSCAPE2.0", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    await ReadNetscapeExtAsync().ConfigureAwait(false);
                                }
                                else
                                {
                                    await SkipAsync().ConfigureAwait(false); // don't care
                                }
                                break;
                            case 0xfe:// comment extension
                                await SkipAsync().ConfigureAwait(false);
                                break;
                            case 0x01:// plain text extension
                                await SkipAsync().ConfigureAwait(false);
                                break;
                            default: // uninteresting extension
                                await SkipAsync().ConfigureAwait(false);
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
            await DecodeBitmapDataAsync().ConfigureAwait(false); // decode pixel data
            await SkipAsync().ConfigureAwait(false);
            if (Err())
            {
                return;
            }
            frameCount++;
            // create new image to receive frame data
            //image = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb4444);
            await SetPixelsAsync().ConfigureAwait(false); // transfer pixel data to image
            frames.Add(new GifFrame(image, delay)); // add image to frame
                                                           // list
            if (transparency)
            {
                act[transIndex] = save;
            }

            ResetFrame();
        }

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

        protected async Task ReadNetscapeExtAsync()
        {
            do
            {
                await ReadBlockAsync().ConfigureAwait(false);
                if (block[0] == 1)
                {
                    // loop count sub-block
                    int b1 = ((int)block[1]) & 0xff;
                    int b2 = ((int)block[2]) & 0xff;
                    loopCount = (b2 << 8) | b1;
                }
            } while ((blockSize > 0) && !Err());
        }

        protected int ReadShort()
        {
            // read 16-bit value, LSB first
            return Read() | (Read() << 8);
        }

        protected void ResetFrame()
        {
            lastDispose = dispose;
            lrx = ix;
            lry = iy;
            lrw = iw;
            lrh = ih;
            lastBitmap1 = currentBitmap;
            lastBgColor = bgColor;
            dispose = 0;
            transparency = false;
            delay = 0;
            lct = null;
        }

        protected async Task SkipAsync()
        {
            do
            {
                await ReadBlockAsync().ConfigureAwait(false);
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
            try
            {
                int headerCount = 0;
                bool expectSecondPart = false;
                int firstRead;
                while ((firstRead = st.ReadByte()) >= 0)
                {
                    if (firstRead == 0x00)
                    {
                        var secondRead = st.ReadByte();
                        if (!expectSecondPart && secondRead == 0x2C)
                        {
                            expectSecondPart = true;
                        }
                        else if (expectSecondPart && secondRead == 0x21 && st.ReadByte() == 0xF9)
                        {
                            headerCount++;
                            expectSecondPart = false;
                        }

                        if (headerCount > 1)
                            return true;
                    }
                }

                return false;
            }
            finally
            {
                st.Position = 0;
            }
        }
    }
}
