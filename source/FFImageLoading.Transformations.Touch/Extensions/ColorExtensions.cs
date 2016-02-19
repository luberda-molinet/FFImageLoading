using System;
using UIKit;

namespace FFImageLoading.Transformations.Extensions
{
	public static class ColorExtensions
	{
		public static UIColor FromHexString(this string c)
		{
			if (string.IsNullOrWhiteSpace(c) || !c.StartsWith("#"))
				throw new ArgumentException ("Invalid color string.", "c");

			switch (c.Length)
			{
				case 9:
					{
						var cuint = Convert.ToUInt32(c.Substring(1), 16);
						var a = (byte)(cuint >> 24);
						var r = (byte)((cuint >> 16) & 0xff);
						var g = (byte)((cuint >> 8) & 0xff);
						var b = (byte)(cuint & 0xff);

						return UIColor.FromRGBA(r, g, b, a);
					}
				case 7:
					{
						var cuint = Convert.ToUInt32(c.Substring(1), 16);
						var r = (byte)((cuint >> 16) & 0xff);
						var g = (byte)((cuint >> 8) & 0xff);
						var b = (byte)(cuint & 0xff);

						return UIColor.FromRGBA(r, g, b, (byte)255);
					}
				case 5:
					{
						var cuint = Convert.ToUInt16(c.Substring(1), 16);
						var a = (byte)(cuint >> 12);
						var r = (byte)((cuint >> 8) & 0xf);
						var g = (byte)((cuint >> 4) & 0xf);
						var b = (byte)(cuint & 0xf);
						a = (byte)(a << 4 | a);
						r = (byte)(r << 4 | r);
						g = (byte)(g << 4 | g);
						b = (byte)(b << 4 | b);

						return UIColor.FromRGBA(r, g, b, a);
					}
				case 4:
					{
						var cuint = Convert.ToUInt16(c.Substring(1), 16);
						var r = (byte)((cuint >> 8) & 0xf);
						var g = (byte)((cuint >> 4) & 0xf);
						var b = (byte)(cuint & 0xf);
						r = (byte)(r << 4 | r);
						g = (byte)(g << 4 | g);
						b = (byte)(b << 4 | b);

						return UIColor.FromRGBA(r, g, b, (byte)255);
					}
				default:
					throw new FormatException(string.Format("The {0} string passed in the c argument is not a recognized Color format.", c));
			}
		}
	}
}

