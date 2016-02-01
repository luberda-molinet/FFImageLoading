using System;

namespace FFImageLoading.Work
{
	public static class WithLoadingResult
	{
		public static WithLoadingResult<T> Encapsulate<T>(T item, LoadingResult result) where T:class
		{
			return new WithLoadingResult<T>(item, result);
		}
	}

	public struct WithLoadingResult<T> where T:class
	{
		private LoadingResult _result;

		public WithLoadingResult(LoadingResult result)
		{
			_result = result;
			Item = null;
		}

		public WithLoadingResult(T item, LoadingResult result)
		{
			_result = result;
			Item = item;
		}

		public LoadingResult Result
		{
			get
			{
				if ((int)_result < 0) // if LoadingResult is below zero then it's an error
					return _result;

				if (Item == null) // even if we have a success LoadingResult, if Item==null then it's an error
					return LoadingResult.Failed;

				return _result; // success
			}
		}

		public T Item { get; private set; }

		public bool HasError
		{
			get
			{
				return (int)Result < 0; // use Result and not _result since it handles Item == null
			}
		}

		public GenerateResult GenerateResult
		{
			get
			{
				if (!HasError)
					return GenerateResult.Success;

				switch (Result) // use Result and not _result since it handles Item == null
				{
					case LoadingResult.Canceled:
						return GenerateResult.Canceled;

					case LoadingResult.InvalidTarget:
						return GenerateResult.InvalidTarget;

					case LoadingResult.Failed:
						return GenerateResult.Failed;

					default:
						return GenerateResult.Failed;
				}
			}
		}
	}
}

