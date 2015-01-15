using System;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using HGR.Mobile.Droid.ImageLoading.IO;
using System.Net.Http;
using HGR.Mobile.Droid.ImageLoading.Helpers;

namespace HGR.Mobile.Droid.ImageLoading.Cache
{
    public class DownloadCache
    {
        private static readonly DiskCache _diskCache;
        private static readonly HttpClient _httpClient;
        private static readonly MD5Helper _md5Helper;

        static DownloadCache()
        {
            _diskCache = DiskCache.CreateCache(Android.App.Application.Context, typeof(DownloadCache).Name);
            _httpClient = new HttpClient(new ModernHttpClient.NativeMessageHandler());
            _md5Helper = new MD5Helper();
        }

        public DownloadCache()
        {

        }

        public Task<byte[]> GetAsync(string url)
        {
            return GetAsync(url, new TimeSpan(30, 0, 0, 0)); // by default we cache data 30 days
        }

        public async Task<byte[]> GetAsync(string url, TimeSpan duration)
        {
            string filename = _md5Helper.MD5(url);
            byte[] data = await _diskCache.TryGet(filename).ConfigureAwait(false);
            if (data != null)
                return data;

            data = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            if (data == null)
                data = new byte[0];

            if (data.Length == 0)
            {
                // this is a strange situation so let's not cache this too long: here 5 minutes
                duration = new TimeSpan(0, 5, 0);
            }

            // this ensures the fullpath exists
            await _diskCache.AddOrUpdate(filename, data, duration).ConfigureAwait(false);
            return data;
        }
    }
}

