using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FFImageLoading.Transformations"), InternalsVisibleTo("FFImageLoading.Svg.Forms")]
namespace FFImageLoading
{
	sealed class PreserveAttribute : System.Attribute
	{
		public bool AllMembers;
		public bool Conditional;
	}
}
