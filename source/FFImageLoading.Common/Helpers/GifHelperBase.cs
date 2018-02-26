using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Work;

namespace FFImageLoading
{
    public abstract class GifHelperBase<TNativeImageContainer>
    {
        object _lock = new object();
        const int STATUS_OK = 0;
        const int STATUS_FORMAT_ERROR = 1;
        const int STATUS_OPEN_ERROR = 2;
        int status;
        const int MAX_STACK_SIZE = 4096;
        Stream input;
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
        TNativeImageContainer image; // current frame
        TaskParameter parameters;
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
        List<GifFrame> frames = new List<GifFrame>(); // frames read from current file
        int frameCount;
        int _downsampleWidth;
        int _downsampleHeight;
        bool _downsample;

        public List<GifFrame> Frames { get { return frames; } }

        protected abstract int DipToPixels(int dips);
        protected abstract Task<TNativeImageContainer> ToBitmapAsync(int[] data, int width, int height, int downsampleWidth, int downsampleHeight);

        async Task SetPixelsAsync()
        {
            int[] result = new int[width * height];

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
                    result = lastBitmap1;

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
                                result[k] = c;
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
                            result[dx] = c;
                        }
                        dx++;
                    }
                }
            }

            currentBitmap = result;
            TNativeImageContainer bitmap;

            int downsampleWidth = 0;
            int downsampleHeight = 0;
            int insample = 1;
            bool downsampled = false;

            if (_downsampleWidth == 0 && _downsampleHeight != 0)
            {
                downsampleWidth = (int)(((float)_downsampleHeight / height) * width);
                downsampled = true;
            }
            else if (_downsampleHeight == 0 && _downsampleWidth != 0)
            {
                downsampleHeight = (int)(((float)_downsampleWidth / width) * height);
                downsampled = true;
            }

            if (downsampled)
            {
                insample = CalculateInSampleSize(width, height, downsampleWidth, downsampleHeight, false);
                bitmap = await ToBitmapAsync(result, width, height, downsampleWidth, downsampleHeight);
            }
            else
            {
                bitmap = await ToBitmapAsync(result, width, height, downsampleWidth, downsampleHeight);
            }

            image = bitmap;
        }

        public static int CalculateInSampleSize(float width, float height, int reqWidth, int reqHeight, bool allowUpscale)
        {
            if (reqWidth == 0)
                reqWidth = (int)((reqHeight / height) * width);

            if (reqHeight == 0)
                reqHeight = (int)((reqWidth / width) * height);

            double inSampleSize = 1d;

            if (height > reqHeight || width > reqWidth || allowUpscale)
            {
                // Calculate ratios of height and width to requested height and width
                int heightRatio = (int)Math.Round(height / reqHeight);
                int widthRatio = (int)Math.Round(width / reqWidth);

                // Choose the smallest ratio as inSampleSize value, this will guarantee
                // a final image with both dimensions larger than or equal to the
                // requested height and width.
                inSampleSize = heightRatio < widthRatio ? heightRatio : widthRatio;
            }

            int x = (int)inSampleSize;

            x = x | (x >> 1);
            x = x | (x >> 2);
            x = x | (x >> 4);
            x = x | (x >> 8);
            x = x | (x >> 16);
            return x - (x >> 1);
        }

        public async Task ReadGifAsync(Stream inputStream, string path, TaskParameter parameters)
        {
            this.parameters = parameters;
            var dip = parameters.DownSampleUseDipUnits;
            _downsample = parameters.DownSampleSize != null;

            if (_downsample)
            {
                _downsampleWidth = parameters.DownSampleSize.Item1;
                _downsampleHeight = parameters.DownSampleSize.Item2;

                if (dip)
                {
                    _downsampleWidth = DipToPixels(_downsampleWidth);
                    _downsampleHeight = DipToPixels(_downsampleHeight);
                }
            }

            if (inputStream != null)
            {
                input = inputStream;
                await ReadHeaderAsync().ConfigureAwait(false);
                if (!Err)
                {
                    await ReadContentsAsync().ConfigureAwait(false);
                    if (frameCount < 0)
                    {
                        throw new Exception("GIF parsing error");
                    }
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (status != STATUS_OK)
                throw new Exception("GIF parsing error");
        }

        async Task DecodeBitmapDataAsync()
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

        TNativeImageContainer GetFrame(int n)
        {
            lock (_lock)
            {
                if (frameCount <= 0)
                    return default(TNativeImageContainer);
                n = n % frameCount;
                return frames[n].Image;
            }
        }

        bool Err => status != STATUS_OK;

        int Read()
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

        async Task<int> ReadBlockAsync()
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

        async Task<int[]> ReadColorTableAsync(int ncolors)
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
                    var rgb = (r << 16) | (g << 8) | b;
                    tab[i++] = (int)(0xff000000 | rgb);
                }
            }
            return tab;
        }

        async Task ReadContentsAsync()
        {
            // read GIF file content blocks
            bool done = false;
            while (!(done || Err))
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
                                if (app.Equals("NETSCAPE2.0", StringComparison.OrdinalIgnoreCase))
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

        void ReadGraphicControlExt()
        {
            Read(); // block size
            int packed = Read(); // packed fields
            dispose = (packed & 0x1c) >> 2; // disposal method
            if (dispose == 0)
            {
                dispose = 1; // elect to keep old image if discretionary
            }
            transparency = (packed & 1) != 0;
            delay = Math.Max(10, ReadShort() * 10); // delay in milliseconds, enforcing min 10ms framerate

            transIndex = Read(); // transparent color index
            Read(); // block terminator
        }

        async Task ReadHeaderAsync()
        {
            String id = "";
            for (int i = 0; i < 6; i++)
            {
                id += (char)Read();
            }
            if (!id.StartsWith("GIF", StringComparison.OrdinalIgnoreCase))
            {
                status = STATUS_FORMAT_ERROR;
                return;
            }
            ReadLSD();
            if (gctFlag && !Err)
            {
                gct = await ReadColorTableAsync(gctSize);
                bgColor = gct[bgIndex];
            }
        }

        async Task ReadBitmapAsync()
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
            if (Err)
            {
                return;
            }
            await DecodeBitmapDataAsync().ConfigureAwait(false); // decode pixel data
            await SkipAsync().ConfigureAwait(false);
            if (Err)
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

        void ReadLSD()
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

        async Task ReadNetscapeExtAsync()
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
            } while ((blockSize > 0) && !Err);
        }

        int ReadShort()
        {
            // read 16-bit value, LSB first
            return Read() | (Read() << 8);
        }

        void ResetFrame()
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

        async Task SkipAsync()
        {
            do
            {
                await ReadBlockAsync().ConfigureAwait(false);
            } while ((blockSize > 0) && !Err);
        }

        public class GifFrame
        {
            public GifFrame(TNativeImageContainer im, int del)
            {
                Image = im;
                Delay = del;
            }

            public TNativeImageContainer Image;
            public int Delay;
        }
    }
}
