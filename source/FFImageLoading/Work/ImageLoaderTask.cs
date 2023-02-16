using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Config;
using System.Linq;
using FFImageLoading.DataResolvers;
using System.Collections.Generic;
using FFImageLoading.Decoders;

namespace FFImageLoading.Work
{
	public abstract class ImageLoaderTask<TDecoderContainer, TImageContainer, TImageView> : IImageLoaderTask where TDecoderContainer : class where TImageContainer : class where TImageView : class
	{
		private bool _isLoadingPlaceholderLoaded;

#pragma warning disable RECS0108 // Warns about static fields in generic types
		protected static readonly SemaphoreSlim _placeholdersResolveLock = new SemaphoreSlim(1, 1);
#pragma warning restore RECS0108 // Warns about static fields in generic types

		public ImageLoaderTask(
			IImageService<TImageContainer> imageService,
			ITarget<TImageContainer, TImageView> target,
			TaskParameter parameters)
		{
			PlatformTarget = target;
			ImageService = imageService;
			Parameters = parameters;
			CancellationTokenSource = new CancellationTokenSource();
			CanUseMemoryCache = true;
			SetKeys();
		}

		public virtual async Task Init()
		{
			if (Parameters.Source == ImageSource.Stream && Configuration.StreamChecksumsAsKeys && string.IsNullOrWhiteSpace(Parameters.CustomCacheKey))
			{
				// Loading placeholder if enabled
				if (!_isLoadingPlaceholderLoaded && !string.IsNullOrWhiteSpace(Parameters.LoadingPlaceholderPath))
				{
					await ShowPlaceholder(Parameters.LoadingPlaceholderPath, KeyForLoadingPlaceholder,
										  Parameters.LoadingPlaceholderSource, true).ConfigureAwait(false);
				}

				try
				{
					Parameters.StreamRead = await (Parameters.Stream?.Invoke(CancellationTokenSource.Token)).ConfigureAwait(false);
				}
				catch (TaskCanceledException ex)
				{
					Parameters.StreamRead = null;
					ImageService.Logger.Error(ex.Message, ex);
				}

				if (Parameters.StreamRead != null && Parameters.StreamRead.CanSeek)
				{
					Parameters.StreamChecksum = ImageService.Md5Helper.MD5(Parameters.StreamRead);
					Parameters.StreamRead.Position = 0;

					SetKeys();
				}
			}
		}

		private void SetKeys()
		{
			KeyRaw = Parameters.Path;

			if (Parameters.Source == ImageSource.Stream)
			{
				if (!string.IsNullOrWhiteSpace(Parameters.StreamChecksum))
				{
					CanUseMemoryCache = true;
					KeyRaw = Parameters.StreamChecksum;
				}
				else
				{
					CanUseMemoryCache = false;
					KeyRaw = string.Concat("Stream_", Guid.NewGuid().ToString("N"));
				}
			}

			var isCustomCacheKeySet = !string.IsNullOrWhiteSpace(Parameters.CustomCacheKey);

			if (isCustomCacheKeySet)
			{
				CanUseMemoryCache = true;
				KeyRaw = Parameters.CustomCacheKey;
			}

			if (Parameters.CacheType == CacheType.Disk || Parameters.CacheType == CacheType.None)
			{
				CanUseMemoryCache = false;
			}

			if (string.IsNullOrWhiteSpace(KeyRaw))
				throw new Exception("Key cannot be null");

			if (Parameters.CustomDataResolver is IVectorDataResolver vect)
			{
				if (vect.ReplaceStringMap == null || vect.ReplaceStringMap.Count == 0)
				{
					KeyRaw = string.Format(
						"{0};(size={1}x{2},type={3})",
						KeyRaw,
						vect.UseDipUnits ? DpiToPixels(vect.VectorWidth, Parameters.Scale) : vect.VectorWidth,
						vect.UseDipUnits ? DpiToPixels(vect.VectorHeight, Parameters.Scale) : vect.VectorHeight,
						vect.GetType().Name);
				}
				else
				{
					KeyRaw = string.Format(
						"{0};(size={1}x{2},replace=({3}),type={4})",
						KeyRaw,
						vect.UseDipUnits ? DpiToPixels(vect.VectorWidth, Parameters.Scale) : vect.VectorWidth,
						vect.UseDipUnits ? DpiToPixels(vect.VectorHeight, Parameters.Scale) : vect.VectorHeight,
						string.Join(",", vect.ReplaceStringMap.Select(x => string.Format("{0}/{1}", x.Key, x.Value)).OrderBy(v => v)),
						vect.GetType().Name);
				}
			}

			if (Parameters.DownSampleSize != null && (Parameters.DownSampleSize.Item1 > 0 || Parameters.DownSampleSize.Item2 > 0))
			{
				KeyDownsamplingOnly = string.Concat(
					";",
					Parameters.DownSampleUseDipUnits ? DpiToPixels(Parameters.DownSampleSize.Item1, Parameters.Scale) : Parameters.DownSampleSize.Item1,
					"x",
					Parameters.DownSampleUseDipUnits ? DpiToPixels(Parameters.DownSampleSize.Item2, Parameters.Scale) : Parameters.DownSampleSize.Item2);
			}
			else
			{
				KeyDownsamplingOnly = string.Empty;
			}

			if (Parameters.Transformations != null && Parameters.Transformations.Count > 0)
			{
				KeyTransformationsOnly = string.Concat(";", string.Join(";", Parameters.Transformations.Select(t => t.Key)));
			}
			else
			{
				KeyTransformationsOnly = string.Empty;
			}

			if (isCustomCacheKeySet)
			{
				Key = Parameters.CustomCacheKey;
				KeyWithoutTransformations = Parameters.CustomCacheKey;
			}
			else
			{
				Key = string.Concat(KeyRaw, KeyDownsamplingOnly, KeyTransformationsOnly);
				KeyWithoutTransformations = string.Concat(KeyRaw, KeyDownsamplingOnly);
			}

			if (!string.IsNullOrWhiteSpace(Parameters.LoadingPlaceholderPath))
			{
				if (TransformPlaceholders)
					KeyForLoadingPlaceholder = string.Concat(Parameters.LoadingPlaceholderPath, KeyDownsamplingOnly, KeyTransformationsOnly);
				else
					KeyForLoadingPlaceholder = string.Concat(Parameters.LoadingPlaceholderPath, KeyDownsamplingOnly);

				if (Parameters.CustomLoadingPlaceholderDataResolver is IVectorDataResolver vectLo)
				{
					if (vectLo.ReplaceStringMap == null || vectLo.ReplaceStringMap.Count == 0)
						KeyForLoadingPlaceholder = string.Format(
							"{0};(size={1}x{2})",
							KeyForLoadingPlaceholder,
							vectLo.UseDipUnits ? DpiToPixels(vectLo.VectorWidth, Parameters.Scale) : vectLo.VectorWidth,
							vectLo.UseDipUnits ? DpiToPixels(vectLo.VectorHeight, Parameters.Scale) : vectLo.VectorHeight);
					else
						KeyForLoadingPlaceholder = string.Format(
							"{0};(size={1}x{2},replace=({3}))",
							KeyForLoadingPlaceholder,
							vectLo.UseDipUnits ? DpiToPixels(vectLo.VectorWidth, Parameters.Scale) : vectLo.VectorWidth,
							vectLo.UseDipUnits ? DpiToPixels(vectLo.VectorHeight, Parameters.Scale) : vectLo.VectorHeight,
							string.Join(",", vectLo.ReplaceStringMap.Select(x => string.Format("{0}/{1}", x.Key, x.Value)).OrderBy(v => v)));
				}
			}

			if (!string.IsNullOrWhiteSpace(Parameters.ErrorPlaceholderPath))
			{
				if (TransformPlaceholders)
					KeyForErrorPlaceholder = string.Concat(Parameters.ErrorPlaceholderPath, KeyDownsamplingOnly, KeyTransformationsOnly);
				else
					KeyForErrorPlaceholder = string.Concat(Parameters.ErrorPlaceholderPath, KeyDownsamplingOnly);

				if (Parameters.CustomLoadingPlaceholderDataResolver is IVectorDataResolver vectEr)
				{
					if (vectEr.ReplaceStringMap == null || vectEr.ReplaceStringMap.Count == 0)
						KeyForErrorPlaceholder = string.Format(
							"{0};(size={1}x{2})",
							KeyForErrorPlaceholder,
							vectEr.UseDipUnits ? DpiToPixels(vectEr.VectorWidth, Parameters.Scale) : vectEr.VectorWidth,
							vectEr.UseDipUnits ? DpiToPixels(vectEr.VectorHeight, Parameters.Scale) : vectEr.VectorHeight);
					else
						KeyForErrorPlaceholder = string.Format(
							"{0};(size={1}x{2},replace=({3}))",
							KeyForErrorPlaceholder,
							vectEr.UseDipUnits ? DpiToPixels(vectEr.VectorWidth, Parameters.Scale) : vectEr.VectorWidth,
							vectEr.UseDipUnits ? DpiToPixels(vectEr.VectorHeight, Parameters.Scale) : vectEr.VectorHeight,
							string.Join(",", vectEr.ReplaceStringMap.Select(x => string.Format("{0}/{1}", x.Key, x.Value)).OrderBy(v => v)));
				}
			}
		}

		private ImageInformation _imageInformation;
		public ImageInformation ImageInformation
		{
			get => _imageInformation;

			private set
			{
				_imageInformation = value;
				ImageInformation.SetKey(Key, Parameters.CustomCacheKey);
				ImageInformation.SetPath(Parameters.Path);
			}
		}

		public DownloadInformation DownloadInformation { get; private set; }

		public ITarget<TImageContainer, TImageView> PlatformTarget { get; private set; }

		public ITarget Target => PlatformTarget as ITarget;

		public IConfiguration Configuration
			=> ImageService.Configuration;

		protected readonly IImageService<TImageContainer> ImageService;
		
		protected CancellationTokenSource CancellationTokenSource { get; private set; }


		protected abstract int DpiToPixels(int size, double scale);

		protected abstract IDecoder<TDecoderContainer> ResolveDecoder(ImageInformation.ImageType type);

		protected abstract Task<TDecoderContainer> TransformAsync(TDecoderContainer bitmap, IList<ITransformation> transformations, string path, ImageSource source, bool isPlaceholder);

		public bool IsCancelled
		{
			get
			{
				try
				{
					return _isDisposed || (CancellationTokenSource != null && CancellationTokenSource.IsCancellationRequested);
				}
				catch (ObjectDisposedException)
				{
					return true;
				}
			}
		}

		public bool CanUseMemoryCache { get; private set; }

		public bool IsCompleted { get; private set; }

		public string Key { get; private set; }

		public string KeyRaw { get; private set; }

		public string KeyWithoutTransformations { get; private set; }

		protected string KeyTransformationsOnly { get; private set; }

		protected string KeyDownsamplingOnly { get; private set; }

		protected string KeyForLoadingPlaceholder { get; private set; }

		protected string KeyForErrorPlaceholder { get; private set; }

		protected WeakReference<TImageContainer> PlaceholderWeakReference { get; private set; }

		protected bool TransformPlaceholders => (Parameters.TransformPlaceholdersEnabled.HasValue && Parameters.TransformPlaceholdersEnabled.Value)
					|| (!Parameters.TransformPlaceholdersEnabled.HasValue && Configuration.TransformPlaceholders);

		public TaskParameter Parameters { get; private set; }

		protected void ThrowIfCancellationRequested()
		{
			try
			{
				CancellationTokenSource?.Token.ThrowIfCancellationRequested();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		public void Cancel()
		{
			if (!_isDisposed)
			{
				if (IsCancelled || IsCompleted)
					return;

				ImageService.RemovePendingTask(this);

				try
				{
					CancellationTokenSource?.Cancel();
				}
				catch (ObjectDisposedException)
				{
				}

				if (Configuration.VerboseLoadingCancelledLogging)
					ImageService.Logger.Debug(string.Format("Image loading cancelled: {0}", Key));
			}
		}

		protected abstract Task<TImageContainer> GenerateImageFromDecoderContainerAsync(IDecodedImage<TDecoderContainer> decoded, ImageInformation imageInformation, bool isPlaceholder);

		protected abstract Task SetTargetAsync(TImageContainer image, bool animated);

		protected virtual void BeforeLoading(TImageContainer image, bool fromMemoryCache) { }

		protected virtual void AfterLoading(TImageContainer image, bool fromMemoryCache) { }

		protected virtual async Task<TImageContainer> GenerateImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
		{
			//using (imageData)
			{
				var decoder = ResolveDecoder(imageInformation.Type);
				var decoderContainer = await decoder.DecodeAsync(
					imageData, path, source, imageInformation, Parameters).ConfigureAwait(false);

				if (enableTransformations && Parameters.Transformations != null && Parameters.Transformations.Count > 0)
				{
					var transformations = Parameters.Transformations.ToList();

					if (decoderContainer.IsAnimated)
					{
						for (var i = 0; i < decoderContainer.AnimatedImages.Length; i++)
						{
							decoderContainer.AnimatedImages[i].Image = await TransformAsync(
								decoderContainer.AnimatedImages[i].Image,
								transformations, path, source, isPlaceholder).ConfigureAwait(false);
						}
					}
					else
					{
						decoderContainer.Image = await TransformAsync(
							decoderContainer.Image, transformations, path, source, isPlaceholder).ConfigureAwait(false);
					}
				}

				return await GenerateImageFromDecoderContainerAsync(decoderContainer, imageInformation, isPlaceholder).ConfigureAwait(false);
			}
		}

		protected virtual async Task<TImageContainer> GenerateImageAsync(string path, ImageSource source, IDecodedImage<object> decoded, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
		{
			var decoderContainer = new DecodedImage<TDecoderContainer>()
			{
				IsAnimated = decoded.IsAnimated,
				Image = decoded.Image as TDecoderContainer,
				AnimatedImages = decoded.AnimatedImages?.Select(
					v => new AnimatedImage<TDecoderContainer> { Delay = v.Delay, Image = v.Image as TDecoderContainer })
										.ToArray()
			};

			if (enableTransformations && Parameters.Transformations != null && Parameters.Transformations.Count > 0)
			{
				var transformations = Parameters.Transformations.ToList();

				if (decoderContainer.IsAnimated)
				{
					for (var i = 0; i < decoderContainer.AnimatedImages.Length; i++)
					{
						decoderContainer.AnimatedImages[i].Image = await TransformAsync(
							decoderContainer.AnimatedImages[i].Image, transformations, path, source, isPlaceholder).ConfigureAwait(false);
					}
				}
				else
				{
					decoderContainer.Image = await TransformAsync(
						decoderContainer.Image, transformations, path, source, isPlaceholder).ConfigureAwait(false);
				}
			}

			return await GenerateImageFromDecoderContainerAsync(decoderContainer, imageInformation, isPlaceholder).ConfigureAwait(false);
		}

		public async virtual Task<bool> TryLoadFromMemoryCacheAsync()
		{
			try
			{
				var cacheType = Parameters?.CacheType ?? CacheType.All;

				if (Parameters.Preload &&
					(cacheType == CacheType.Disk || cacheType == CacheType.None))
					return false;

				ThrowIfCancellationRequested();

				var isFadeAnimationEnabledForCached = Parameters.FadeAnimationForCachedImagesEnabled ?? Configuration.FadeAnimationForCachedImages;
				var result = await TryLoadFromMemoryCacheAsync(Key, true, isFadeAnimationEnabledForCached, false).ConfigureAwait(false);

				if (result)
				{
					ImageService.Logger.Debug(string.Format("Image loaded from cache: {0}", Key));
					IsCompleted = true;

					if (Configuration.ExecuteCallbacksOnUIThread && (Parameters?.OnSuccess != null || Parameters?.OnFinish != null))
					{
						await ImageService.Dispatcher.PostAsync(() =>
						{
							Parameters?.OnSuccess?.Invoke(ImageInformation, LoadingResult.MemoryCache);
							Parameters?.OnFinish?.Invoke(this);
						}).ConfigureAwait(false);
					}
					else
					{
						Parameters?.OnSuccess?.Invoke(ImageInformation, LoadingResult.MemoryCache);
						Parameters?.OnFinish?.Invoke(this);
					}
				}
				else
				{
					ThrowIfCancellationRequested();
					// Loading placeholder if enabled
					if (!_isLoadingPlaceholderLoaded && !string.IsNullOrWhiteSpace(Parameters.LoadingPlaceholderPath))
					{
						await ShowPlaceholder(Parameters.LoadingPlaceholderPath, KeyForLoadingPlaceholder,
											  Parameters.LoadingPlaceholderSource, true).ConfigureAwait(false);
					}
				}

				return result;
			}
			catch (Exception ex)
			{
				if (Configuration.ClearMemoryCacheOnOutOfMemory && ex is OutOfMemoryException)
				{
					ImageService.MemoryCache.Clear();
				}

				if (ex is OperationCanceledException)
				{
					if (Configuration.VerboseLoadingCancelledLogging)
					{
						ImageService.Logger.Debug(string.Format("Image loading cancelled: {0}", Key));
					}
				}
				else
				{
					ImageService.Logger.Error(string.Format("Image loading failed: {0}", Key), ex);

					if (Configuration.ExecuteCallbacksOnUIThread && Parameters?.OnError != null)
					{
						await ImageService.Dispatcher.PostAsync(() =>
						{
							Parameters?.OnError?.Invoke(ex);
						}).ConfigureAwait(false);
					}
					else
					{
						Parameters?.OnError?.Invoke(ex);
					}
				}
			}

			return false;
		}

		private async Task<bool> TryLoadFromMemoryCacheAsync(string key, bool updateImageInformation, bool animated, bool isLoadingPlaceholder)
		{
			var found = ImageService.MemoryCache.Get(key);
			if (found?.Item1 != null)
			{
				try
				{
					BeforeLoading(found.Item1, true);

					if (isLoadingPlaceholder)
						PlaceholderWeakReference = new WeakReference<TImageContainer>(found.Item1);

					ThrowIfCancellationRequested();

					if (Target != null)
						await SetTargetAsync(found.Item1, animated).ConfigureAwait(false);

					if (updateImageInformation)
					{
						ImageInformation = found.Item2;
					}
				}
				finally
				{
					AfterLoading(found.Item1, true);
				}

				return true;
			}

			return false;
		}

		protected virtual async Task ShowPlaceholder(string path, string key, ImageSource source, bool isLoadingPlaceholder)
		{
			if (Parameters.Preload)
				return;

			if (!await TryLoadFromMemoryCacheAsync(key, false, false, isLoadingPlaceholder).ConfigureAwait(false))
			{
				try
				{
					var customResolver = isLoadingPlaceholder ? Parameters.CustomLoadingPlaceholderDataResolver : Parameters.CustomErrorPlaceholderDataResolver;
					var loadResolver = customResolver ?? ImageService.DataResolverFactory.GetResolver(path, source, Parameters);
					loadResolver = new WrappedDataResolver(loadResolver);
					DataResolverResult loadImageData;
					TImageContainer loadImage;

					if (!await _placeholdersResolveLock.WaitAsync(TimeSpan.FromSeconds(10), CancellationTokenSource.Token).ConfigureAwait(false))
						return;

					try
					{
						ThrowIfCancellationRequested();

						if (await TryLoadFromMemoryCacheAsync(key, false, false, isLoadingPlaceholder).ConfigureAwait(false))
						{
							if (isLoadingPlaceholder)
							{
								_isLoadingPlaceholderLoaded = true;
								Parameters.OnLoadingPlaceholderSet?.Invoke();
							}

							return;
						}

						ThrowIfCancellationRequested();
						loadImageData = await loadResolver.Resolve(path, Parameters, CancellationTokenSource.Token).ConfigureAwait(false);
						ThrowIfCancellationRequested();

						if (loadImageData.Stream != null)
						{
							//using (loadImageData.Stream)
							{
								loadImage = await GenerateImageAsync(path, source, loadImageData.Stream, loadImageData.ImageInformation, TransformPlaceholders, true).ConfigureAwait(false);
							}
						}
						else
						{
							loadImage = await GenerateImageAsync(path, source, loadImageData.Decoded, loadImageData.ImageInformation, TransformPlaceholders, true).ConfigureAwait(false);
						}

						if (loadImage != default(TImageContainer))
							ImageService.MemoryCache.Add(key, loadImageData.ImageInformation, loadImage);
					}
					finally
					{
						_placeholdersResolveLock.Release();
					}

					ThrowIfCancellationRequested();

					if (isLoadingPlaceholder)
						PlaceholderWeakReference = new WeakReference<TImageContainer>(loadImage);

					if (Target != null)
						await SetTargetAsync(loadImage, false).ConfigureAwait(false);

					if (isLoadingPlaceholder)
					{
						_isLoadingPlaceholderLoaded = true;
						Parameters.OnLoadingPlaceholderSet?.Invoke();
					}
				}
				catch (Exception ex)
				{
					if (ex is OperationCanceledException)
						throw;

					ImageService.Logger.Error("Setting placeholder failed", ex);
				}
			}
			else if (isLoadingPlaceholder)
			{
				_isLoadingPlaceholderLoaded = true;
				Parameters.OnLoadingPlaceholderSet?.Invoke();
			}
		}

		public async Task RunAsync()
		{
			var loadingResult = LoadingResult.Failed;
			var success = false;

			try
			{
				// LOAD IMAGE
				if (!(await TryLoadFromMemoryCacheAsync().ConfigureAwait(false)))
				{
					if (Parameters.DelayInMs.HasValue && Parameters.DelayInMs.Value > 0)
					{
						await Task.Delay(Parameters.DelayInMs.Value).ConfigureAwait(false);
					}
					else if (!Parameters.Preload && Configuration.DelayInMs > 0)
					{
						await Task.Delay(Configuration.DelayInMs).ConfigureAwait(false);
					}

					ImageService.Logger.Debug(string.Format("Generating/retrieving image: {0}", Key));
					var resolver = Parameters.CustomDataResolver ?? ImageService.DataResolverFactory.GetResolver(Parameters.Path, Parameters.Source, Parameters);
					resolver = new WrappedDataResolver(resolver);
					var imageData = await resolver.Resolve(Parameters.Path, Parameters, CancellationTokenSource.Token).ConfigureAwait(false);
					loadingResult = imageData.LoadingResult;

					ImageInformation = imageData.ImageInformation;
					ThrowIfCancellationRequested();

					// Preload
					if (Parameters.Preload && Parameters.CacheType.HasValue && Parameters.CacheType.Value == CacheType.Disk)
					{
						if (loadingResult == LoadingResult.Internet)
							ImageService.Logger?.Debug(string.Format("DownloadOnly success: {0}", Key));

						success = true;

						return;
					}

					ThrowIfCancellationRequested();

					TImageContainer image;

					if (imageData.Stream != null)
					{
						using (imageData.Stream)
						{
							image = await GenerateImageAsync(Parameters.Path, Parameters.Source, imageData.Stream, imageData.ImageInformation, true, false).ConfigureAwait(false);
						}
					}
					else
					{
						image = await GenerateImageAsync(Parameters.Path, Parameters.Source, imageData.Decoded, imageData.ImageInformation, true, false).ConfigureAwait(false);
					}

					ThrowIfCancellationRequested();

					try
					{
						BeforeLoading(image, false);

						if (image != default(TImageContainer) && CanUseMemoryCache)
							ImageService.MemoryCache.Add(Key, imageData.ImageInformation, image);

						ThrowIfCancellationRequested();

						var isFadeAnimationEnabled = Parameters.FadeAnimationEnabled ?? Configuration.FadeAnimationEnabled;

						if (Target != null)
							await SetTargetAsync(image, isFadeAnimationEnabled).ConfigureAwait(false);
					}
					finally
					{
						AfterLoading(image, false);
					}
				}

				success = true;
			}
			catch (Exception ex)
			{
				if (ex is OperationCanceledException || ex is ObjectDisposedException)
				{
					if (Configuration.VerboseLoadingCancelledLogging)
					{
						ImageService.Logger.Debug(string.Format("Image loading cancelled: {0}", Key));
					}
				}
				else
				{
					if (Configuration.ClearMemoryCacheOnOutOfMemory && ex is OutOfMemoryException)
					{
						ImageService.MemoryCache.Clear();
					}

					ImageService.Logger.Error(string.Format("Image loading failed: {0}", Key), ex);

					if (Configuration.ExecuteCallbacksOnUIThread && Parameters?.OnError != null)
					{
						await ImageService.Dispatcher.PostAsync(() =>
						{
							Parameters?.OnError?.Invoke(ex);
						}).ConfigureAwait(false);
					}
					else
					{
						Parameters?.OnError?.Invoke(ex);
					}

					try
					{
						// Error placeholder if enabled
						if (!Parameters.Preload && !string.IsNullOrWhiteSpace(Parameters.ErrorPlaceholderPath))
						{
							await ShowPlaceholder(Parameters.ErrorPlaceholderPath, KeyForErrorPlaceholder,
												  Parameters.ErrorPlaceholderSource, false).ConfigureAwait(false);
						}
					}
					catch (Exception ex2)
					{
						if (!(ex2 is OperationCanceledException))
						{
							ImageService.Logger.Error(string.Format("Image loading failed: {0}", Key), ex);
						}
					}
				}
			}
			finally
			{
				try
				{
					if (CancellationTokenSource?.IsCancellationRequested == false)
						CancellationTokenSource.Cancel();
				}
				catch { }

				IsCompleted = true;

				using (Parameters)
				{
					if (Configuration.ExecuteCallbacksOnUIThread && Parameters?.OnFinish != null)
					{
						await ImageService.Dispatcher.PostAsync(() =>
						{
							if (success)
								Parameters?.OnSuccess?.Invoke(ImageInformation, loadingResult);
							Parameters?.OnFinish?.Invoke(this);
						}).ConfigureAwait(false);
					}
					else
					{
						if (success)
							Parameters?.OnSuccess?.Invoke(ImageInformation, loadingResult);
						Parameters?.OnFinish?.Invoke(this);
					}

					ImageService.RemovePendingTask(this);
				}
			}
		}

		private bool _isDisposed;

		public void Dispose()
		{
			if (!_isDisposed)
			{
				_isDisposed = true;

				Parameters.TryDispose();
				CancellationTokenSource.TryDispose();
			}
		}
	}
}
