using System;
using FFImageLoading.Work;

namespace FFImageLoading.Config
{
	/// <summary>
	/// Multiplatform configuration.
	/// </summary>
	public class MultiplatformConfiguration
	{
		public MultiplatformConfiguration()
		{
			// default values here:

			MaxCacheSize = 0; 
			LoadWithTransparencyChannel = false;
			FadeAnimationEnabled = true;
			TransformPlaceholders = true;
			DownsampleInterpolationMode = InterpolationMode.Default;
			HttpHeadersTimeout = 15;
			HttpReadTimeout = 30;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FFImageLoading.Config.Configuration"/> loads images with transparency channel. On Android we save 50% of the memory without transparency since we use 2 bytes per pixel instead of 4.
		/// </summary>
		/// <value><c>true</c> if FFIMageLoading loads images with transparency; otherwise, <c>false</c>.</value>
		public bool LoadWithTransparencyChannel { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FFImageLoading.Config.Configuration"/> fade animation enabled.
		/// </summary>
		/// <value><c>true</c> if fade animation enabled; otherwise, <c>false</c>.</value>
		public bool FadeAnimationEnabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FFImageLoading.Config.Configuration"/> transforming place is enabled.
		/// </summary>
		/// <value><c>true</c> if transform should be applied to placeholder images; otherwise, <c>false</c>.</value>
		public bool TransformPlaceholders { get; set; }

		/// <summary>
		/// Gets or sets default downsample interpolation mode.
		/// </summary>
		/// <value>downsample interpolation mode</value>
		public InterpolationMode DownsampleInterpolationMode { get; set; }

		/// <summary>
		/// Gets or sets the maximum time in seconds to wait to receive HTTP headers before the HTTP request is cancelled.
		/// </summary>
		/// <value>The http connect timeout.</value>
		public int HttpHeadersTimeout { get; set; }

		/// <summary>
		/// Gets or sets the maximum time in seconds to wait before the HTTP request is cancelled.
		/// </summary>
		/// <value>The http read timeout.</value>
		public int HttpReadTimeout { get; set; }

		/// <summary>
		/// Gets or sets the maximum size of the cache in bytes.
		/// </summary>
		/// <value>The maximum size of the cache in bytes.</value>
		public int MaxCacheSize { get; set; }
	}
}

