using System;
using Xamarin.Forms;

namespace FFImageLoading.Forms
{
	/// <summary>
	/// CachedImage - Xamarin.Forms image replacement with caching and downsampling capabilities
	/// </summary>
	public class CachedImage : Image
	{
		public static readonly BindablePropertyKey IsLoadingPropertyKey = BindableProperty.CreateReadOnly("IsLoading", typeof(bool), typeof(CachedImage), false, BindingMode.OneWayToSource, null, null, null, null, null);

		public static readonly BindableProperty RetryCountProperty = BindableProperty.Create<CachedImage, int> (w => w.RetryCount, 0);

		/// <summary>
		/// If image loading fails automatically retry it a number of times, with a specific delay. Sets number of retries.
		/// </summary>
		public int RetryCount
		{
			get
			{
				return (int)GetValue(RetryCountProperty); 
			}
			set
			{
				SetValue(RetryCountProperty, value); 
			}
		}

		public static readonly BindableProperty RetryDelayProperty = BindableProperty.Create<CachedImage, int> (w => w.RetryDelay, 250);

		/// <summary>
		/// If image loading fails automatically retry it a number of times, with a specific delay. Sets delay in milliseconds between each trial
		/// </summary>
		public int RetryDelay
		{
			get
			{
				return (int)GetValue(RetryDelayProperty); 
			}
			set
			{
				SetValue(RetryDelayProperty, value); 
			}
		}

		public static readonly BindableProperty DownsampleWidthProperty = BindableProperty.Create<CachedImage, double> (w => w.DownsampleWidth, 0f);

		/// <summary>
		/// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect. 
		/// Optional DownsampleWidth parameter, if value is higher than zero it will try to downsample to this width while keeping aspect ratio.
		/// </summary>
		public double DownsampleWidth
		{
			get
			{
				return (double)GetValue(DownsampleWidthProperty); 
			}
			set
			{
				SetValue(DownsampleWidthProperty, value); 
			}
		}

		public static readonly BindableProperty DownsampleHeightProperty = BindableProperty.Create<CachedImage, double> (w => w.DownsampleHeight, 0f);

		/// <summary>
		/// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect. 
		/// Optional DownsampleHeight parameter, if value is higher than zero it will try to downsample to this height while keeping aspect ratio.
		/// </summary>
		public double DownsampleHeight
		{
			get
			{
				return (double)GetValue(DownsampleHeightProperty); 
			}
			set
			{
				SetValue(DownsampleHeightProperty, value); 
			}
		}

		public static readonly BindableProperty CacheDurationProperty = BindableProperty.Create<CachedImage, TimeSpan> (w => w.CacheDuration, TimeSpan.FromDays(90));

		/// <summary>
		/// How long the file will be cached on disk.
		/// </summary>
		public TimeSpan CacheDuration
		{
			get
			{
				return (TimeSpan)GetValue(CacheDurationProperty); 
			}
			set
			{
				SetValue(CacheDurationProperty, value); 
			}
		}

		public static readonly BindableProperty TransparencyEnabledProperty = BindableProperty.Create<CachedImage, bool> (w => w.TransparencyEnabled, false);

		/// <summary>
		/// Indicates if the transparency channel should be loaded. By default this value comes from ImageService.Config.LoadWithTransparencyChannel.
		/// </summary>
		public bool TransparencyEnabled
		{
			get
			{
				return (bool)GetValue(TransparencyEnabledProperty); 
			}
			set
			{
				SetValue(TransparencyEnabledProperty, value); 
			}
		}
	}
}

