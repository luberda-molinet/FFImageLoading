using System;

namespace FFImageLoading.Work
{
	public enum LoadingPriority
	{
		Lowest = Int32.MinValue,
		Low = -50,
		Normal = 0,
		High = 50,
		Highest = Int32.MaxValue
	}
}

