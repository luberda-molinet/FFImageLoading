using System;
using FFImageLoading.Work;
using Android.Graphics;
using Android.Content;

namespace FFImageLoading.Transformations
{
	public class BlurredTransformation: TransformationBase
	{
		private double _radius;
		private Context _context;

		public BlurredTransformation(double radius)
		{
			_radius = radius;
			_context = Android.App.Application.Context;
		}

		public override string Key
		{
			get { return string.Format("BlurredTransformation, radius = {0}", _radius); }
		}

		protected override Bitmap Transform(Bitmap source)
		{
			return ToBlurred(source, _context, (float)_radius);
		}

		public static Bitmap ToBlurred(Bitmap source, Context context, float radius)
		{
			if ((int)Android.OS.Build.VERSION.SdkInt >= 17)
			{
				Bitmap bitmap = Bitmap.CreateBitmap(source.Width, source.Height, Bitmap.Config.Argb8888);

				using (Canvas canvas = new Canvas(bitmap))
				{
					canvas.DrawBitmap(source, 0, 0, null);
					using (Android.Renderscripts.RenderScript rs = Android.Renderscripts.RenderScript.Create(context))
					{
						using (Android.Renderscripts.Allocation overlayAlloc = Android.Renderscripts.Allocation.CreateFromBitmap(rs, bitmap))
						{
							using (Android.Renderscripts.ScriptIntrinsicBlur blur = Android.Renderscripts.ScriptIntrinsicBlur.Create(rs, overlayAlloc.Element))
							{
								blur.SetInput(overlayAlloc);
								blur.SetRadius(radius);	
								blur.ForEach(overlayAlloc);
								overlayAlloc.CopyTo(bitmap);

								rs.Destroy();
								return bitmap;
							}
						}
					}
				}
			}

			return ToLegacyBlurred(source, context, (int)radius);
		}

		// Source: http://incubator.quasimondo.com/processing/superfast_blur.php
		public static Bitmap ToLegacyBlurred(Bitmap source, Context context, int radius)
		{
			Bitmap img = source.Copy(source.GetConfig(), true);

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
					pix[yi] = (int)((0xff000000 & pix[yi]) | (dv[rsum] << 16) | (dv[gsum] << 8) | dv[bsum]);
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

