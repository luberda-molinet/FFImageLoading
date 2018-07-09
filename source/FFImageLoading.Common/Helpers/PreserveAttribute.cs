using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FFImageLoading.Transformations"),
	InternalsVisibleTo("FFImageLoading.Svg.Forms"),
	InternalsVisibleTo("FFImageLoading.Transformations"),
	InternalsVisibleTo("FFImageLoading.Forms.Sample.Droid"),
	InternalsVisibleTo("FFImageLoading.Forms.Sample.Droid"),
	InternalsVisibleTo("FFImageLoading.Forms.Sample.Droid"),
	InternalsVisibleTo("FFImageLoading.Forms.Sample.Droid"),
	InternalsVisibleTo("FFImageLoading.Forms.Sample.Droid"),
	InternalsVisibleTo("FFImageLoading.Forms.Sample.Droid"),
	InternalsVisibleTo("FFImageLoading.Forms.Sample.Droid"),
	]
namespace FFImageLoading
{
	sealed class PreserveAttribute : System.Attribute
	{
		public bool AllMembers;
		public bool Conditional;
	}
}
