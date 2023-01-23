using System;
using Android.Graphics;
using Android.Content;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class BlurredTransformation: TransformationBase
    {
        private Context _context;

        public BlurredTransformation()
        {
            Radius = 25d;
            _context = new ContextWrapper(Android.App.Application.Context);
        }

        public BlurredTransformation(double radius)
        {
            Radius = radius;
            _context = new ContextWrapper(Android.App.Application.Context);
        }

        double _radius;
        public double Radius
        {
            get
            {
                return _radius;
            }

            set
            {
                _radius = Math.Min(25, Math.Max(value, 0));
            }
        }

        public static bool LegacyMode { get; set; } = false;

        public override string Key
        {
            get { return string.Format("BlurredTransformation,radius={0}", Radius); }
        }

        protected override Bitmap Transform(Bitmap sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToBlurred(sourceBitmap, _context, (float)Radius);
        }

        public static Bitmap ToBlurred(Bitmap source, Context context, float radius)
        {
            if (context != null && !LegacyMode && (int)Android.OS.Build.VERSION.SdkInt >= 17)
            {
                Bitmap output = Bitmap.CreateBitmap(source.Width, source.Height, Bitmap.Config.Argb8888);

                using (Android.Renderscripts.RenderScript rs = Android.Renderscripts.RenderScript.Create(context))
                using (Android.Renderscripts.ScriptIntrinsicBlur script = Android.Renderscripts.ScriptIntrinsicBlur.Create(rs, Android.Renderscripts.Element.U8_4(rs)))
                using (Android.Renderscripts.Allocation inAlloc = Android.Renderscripts.Allocation.CreateFromBitmap(rs, source, Android.Renderscripts.Allocation.MipmapControl.MipmapNone, Android.Renderscripts.AllocationUsage.Script))
                using (Android.Renderscripts.Allocation outAlloc = Android.Renderscripts.Allocation.CreateFromBitmap(rs, output))
                {
                    script.SetRadius(radius);
                    script.SetInput(inAlloc);
                    script.ForEach(outAlloc);
                    outAlloc.CopyTo(output);

                    rs.Destroy();
                    return output;
                }

                //Bitmap output = Bitmap.createBitmap(smallBitmap.getWidth(), smallBitmap.getHeight(), smallBitmap.getConfig());

                //RenderScript rs = RenderScript.create(getContext());
                //ScriptIntrinsicBlur script = ScriptIntrinsicBlur.create(rs, Element.U8_4(rs));
                //Allocation inAlloc = Allocation.createFromBitmap(rs, smallBitmap, Allocation.MipmapControl.MIPMAP_NONE, Allocation.USAGE_GRAPHICS_TEXTURE);
                //Allocation outAlloc = Allocation.createFromBitmap(rs, output);
                //script.setRadius(BLUR_RADIUS);
                //script.setInput(inAlloc);
                //script.forEach(outAlloc);
                //outAlloc.copyTo(output);

                //rs.destroy();
            }

            return ToLegacyBlurred(source, context, (int)radius);
        }

        // Source: http://incubator.quasimondo.com/processing/superfast_blur.php
        public static Bitmap ToLegacyBlurred(Bitmap source, Context context, int radius)
        {
            var config = source.GetConfig();
            if (config == null)
                config = Bitmap.Config.Argb8888;    // This will support transparency

            Bitmap img = source.Copy(config, true);

            int w = img.Width;
            int h = img.Height;
            int wm = w-1;
            int hm = h-1;
            int wh = w*h;
            int div = radius+radius+1;
            int[] r = new int[wh];
            int[] g = new int[wh];
            int[] b = new int[wh];
            int rsum,gsum,bsum,x,y,i,p,p1,p2,yp,yi,yw;
            int[] vmin = new int[Math.Max(w,h)];
            int[] vmax = new int[Math.Max(w,h)];
            int[] pix= new int[w*h];

            img.GetPixels(pix, 0, w, 0,0,w, h);

            int[] dv = new int[256*div];
            for (i=0;i<256*div;i++){
                dv[i]=(i/div);
            }

            yw=yi=0;

            for (y=0;y<h;y++){
                rsum=gsum=bsum=0;
                for(i=-radius;i<=radius;i++){
                    p=pix[yi+Math.Min(wm,Math.Max(i,0))];
                    rsum+=(p & 0xff0000)>>16;
                    gsum+=(p & 0x00ff00)>>8;
                    bsum+= p & 0x0000ff;
                }
                for (x=0;x<w;x++){

                    r[yi]=dv[rsum];
                    g[yi]=dv[gsum];
                    b[yi]=dv[bsum];

                    if(y==0){
                        vmin[x]=Math.Min(x+radius+1,wm);
                        vmax[x]=Math.Max(x-radius,0);
                    }
                    p1=pix[yw+vmin[x]];
                    p2=pix[yw+vmax[x]];

                    rsum+=((p1 & 0xff0000)-(p2 & 0xff0000))>>16;
                    gsum+=((p1 & 0x00ff00)-(p2 & 0x00ff00))>>8;
                    bsum+= (p1 & 0x0000ff)-(p2 & 0x0000ff);
                    yi++;
                }
                yw+=w;
            }

            for (x=0;x<w;x++){
                rsum=gsum=bsum=0;
                yp=-radius*w;
                for(i=-radius;i<=radius;i++){
                    yi=Math.Max(0,yp)+x;
                    rsum+=r[yi];
                    gsum+=g[yi];
                    bsum+=b[yi];
                    yp+=w;
                }
                yi=x;
                for (y=0;y<h;y++){
                    // Preserve alpha channel: ( 0xff000000 & pix[yi] )
                    var rgb = (dv[rsum] << 16) | (dv[gsum] << 8) | dv[bsum];
                    pix[yi] = ((int)(0xff000000 & pix[yi]) | rgb);
                    if(x==0){
                        vmin[y]=Math.Min(y+radius+1,hm)*w;
                        vmax[y]=Math.Max(y-radius,0)*w;
                    }
                    p1=x+vmin[y];
                    p2=x+vmax[y];

                    rsum+=r[p1]-r[p2];
                    gsum+=g[p1]-g[p2];
                    bsum+=b[p1]-b[p2];

                    yi+=w;
                }
            }

            img.SetPixels(pix,0, w,0,0,w,h);
            return img;
        }
    }
}

