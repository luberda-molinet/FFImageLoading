using System;

namespace FFImageLoading.Work
{
	public static class WithLoadingResult
	{
		public static WithLoadingResult<T> Encapsulate<T>(T item, LoadingResult result)
		{
			return new WithLoadingResult<T>(item, result);
		}
	}

	public class WithLoadingResult<T>
	{
		public WithLoadingResult(T item, LoadingResult result)
		{
			Item = item;
			Result = result;
		}

		public LoadingResult Result { get; private set; }

		public T Item { get; private set; }
	}
}

