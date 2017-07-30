using System;

namespace FFImageLoading.Transformations
{
    public sealed class PreserveAttribute : Attribute
	{
		public bool AllMembers;
		public bool Conditional;
	}
}
