using System;

namespace FFImageLoading.Forms
{
	public sealed class PreserveAttribute : System.Attribute
	{
		public bool AllMembers;
		public bool Conditional;
	}
}
