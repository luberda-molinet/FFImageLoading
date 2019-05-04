using System;
namespace FFImageLoading
{
	public static class ArrayExtensions
	{
		public static void Fill<T>(this T[] originalArray, T with)
		{
			for (int i = 0; i < originalArray.Length; i++)
			{
				originalArray[i] = with;
			}
		}

		public static void Fill<T>(this T[] originalArray, int fromIdx, int toIdx, T with)
		{
			if (fromIdx >= originalArray.Length)
				return;

			for (int i = fromIdx; i <= toIdx; i++)
			{
				originalArray[i] = with;
			}
		}
	}
}
