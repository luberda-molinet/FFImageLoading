//
// ByteBoundStringLruCache.cs
//
// Author:
//   Brett Duncavage <brett.duncavage@rd.io>
//
// Copyright 2014 Rdio, Inc.
//
using System;

namespace FFImageLoading.Collections
{
	public interface IByteSizeAware
	{
		long SizeInBytes { get; }
	}

	public class ByteBoundStrongLruCache<TKey, TValue> : StrongLruCache<TKey, TValue> where TValue : IByteSizeAware
	{
		private const string TAG = "ByteBoundStrongLruCache";

		private readonly object monitor = new object();

		private readonly long high_watermark;
		private readonly long low_watermark;
		private long current_cache_size;
		private bool has_exceeded_high_watermark;

		public long CacheSizeInBytes {
			get { lock (monitor) { return current_cache_size; } }
		}

		public ByteBoundStrongLruCache(long highWatermark, long lowWatermark)
		{
			high_watermark = Math.Max(0, highWatermark);
			low_watermark = Math.Max(0, lowWatermark);
			if (high_watermark == 0) {
				throw new ArgumentException("highWatermark must be > 0");
			}
			if (low_watermark == 0) {
				throw new ArgumentException("lowWatermark must be > 0");
			}
			if (high_watermark < low_watermark) {
				high_watermark = low_watermark;
			}
		}

		protected override void OnEntryAdded(TKey key, TValue value)
		{
			lock (monitor) {
				current_cache_size += value.SizeInBytes;
			}
			base.OnEntryAdded(key, value);
		}

		protected override void OnEntryRemoved(bool evicted, TKey key, TValue oldValue, TValue newValue)
		{
			base.OnEntryRemoved(evicted, key, oldValue, newValue);
			// We handle updating the size due to evictions in OnEntryEvicted.
			if (!evicted) {
				lock (monitor) {
					current_cache_size -= oldValue.SizeInBytes;
				}
			}
		}

		protected override void OnWillEvictEntry(TKey key, TValue value)
		{
			// This method is called inside of a lock.
			current_cache_size -= value.SizeInBytes;
		}

		protected override bool CheckEvictionRequired()
		{
			lock (monitor) {
				if (current_cache_size > high_watermark) {
					has_exceeded_high_watermark = true;
					return true;
				} else if (has_exceeded_high_watermark && current_cache_size > low_watermark) {
					return true;
				}
				has_exceeded_high_watermark = false;
			}
			return false;
		}
	}
}

