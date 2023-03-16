using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFImageLoading;
using FFImageLoading.Cache;

namespace Sample
{
	public partial class ByteArrayListPageModel : ObservableObject
	{
		public ByteArrayListPageModel()
		{
		}

		[ObservableProperty]
		ListItem selectedItem;

		[RelayCommand]
		public void ItemSelected()
			=> SelectedItem = null;

        public ObservableCollection<ListItem> Items { get; set; } = new();

        public async void Reload()
        {
            Items.Clear();

            string[] images = {
                "https://farm9.staticflickr.com/8625/15806486058_7005d77438.jpg",
                "https://farm5.staticflickr.com/4011/4308181244_5ac3f8239b.jpg",
                "https://farm8.staticflickr.com/7423/8729135907_79599de8d8.jpg",
                "https://farm3.staticflickr.com/2475/4058009019_ecf305f546.jpg",
                "https://farm6.staticflickr.com/5117/14045101350_113edbe20b.jpg",
                "https://farm8.staticflickr.com/7351/16355627795_204bf423e9.jpg",
                "https://farm8.staticflickr.com/7524/15620725287_3357e9db03.jpg",
                "https://farm9.staticflickr.com/8351/8299022203_de0cb894b0.jpg",
                "https://farm4.staticflickr.com/3688/10684479284_211f2a8b0f.jpg",
                "https://farm6.staticflickr.com/5755/20725502975_0dd9b4c5f2.jpg",
                "https://farm4.staticflickr.com/3732/9308209014_ea8eac4387.jpg",
                "https://farm4.staticflickr.com/3026/3096284216_8b2e8da102.jpg",
                "https://farm3.staticflickr.com/2915/14139578975_42d87d2d00.jpg",
                "https://farm4.staticflickr.com/3900/14949063062_a92fc5426f.jpg",
                "https://farm9.staticflickr.com/8514/8349332314_e1ae376fd4.jpg",
                "https://farm3.staticflickr.com/2241/2513217764_740fdd6afa.jpg",
                "https://farm6.staticflickr.com/5083/5377978827_51d978d271.jpg",
                "https://farm4.staticflickr.com/3626/3499605313_a9d43c1c83.jpg",
                "https://farm1.staticflickr.com/16/19438696_f103861437.jpg",
                "https://farm8.staticflickr.com/7338/8719134406_74a21b617c.jpg",
                "https://farm6.staticflickr.com/5149/5626285743_ae6a75dde7.jpg",
                "https://farm5.staticflickr.com/4105/4963731276_a10e1bd520.jpg",
                "https://farm4.staticflickr.com/3270/2814060518_6305796eb1.jpg",
                "https://farm7.staticflickr.com/6183/6123785115_2c17b85328.jpg",
                "https://farm3.staticflickr.com/2900/14398989204_59f60a05c5.jpg",
                "https://farm3.staticflickr.com/2136/1756449787_c93e6eb647.jpg",
                "https://farm4.staticflickr.com/3201/3070391067_c80fb9e942.jpg"
            };

			var imageService = MauiProgram.Services.GetRequiredService<IImageService<TImageContainer>>();

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i < images.Length; i++)
                {
                    // Get your byte array here
                    byte[] byteArray;
                    CacheStream cacheStream = null;

                    try
                    {
	                    cacheStream = await imageService.DownloadCache.DownloadAndCacheIfNeededAsync(
		                    images[i], imageService.LoadUrl(images[i]), imageService.Configuration,
		                    CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
	                    Debug.WriteLine($"{ex.Message}");

                    }

                    if (cacheStream != null)
                    {
	                    using (cacheStream.ImageStream)
	                    using (var memoryStream = new MemoryStream())
	                    {
		                    await cacheStream.ImageStream.CopyToAsync(memoryStream);
		                    byteArray = memoryStream.ToArray();
	                    }

	                    var item1 = new ListItem()
	                    {
		                    ImageSource = new CustomStreamImageSource()
		                    {
			                    Key = images[i],
			                    Stream = (token) => Task.FromResult<Stream>(new MemoryStream(byteArray))
		                    },
		                    FileName = string.Format("image{0}.jpeg", i + 1),
	                    };
	                    Items.Add(item1);

	                    var item2 = new ListItem()
	                    {
		                    ImageSource = new CustomStreamImageSource()
		                    {
			                    Key = images[i],
			                    Stream = (token) => Task.FromResult<Stream>(new MemoryStream(byteArray))
		                    },
		                    FileName = string.Format("image{0}.jpeg", i + 1),
	                    };
	                    Items.Add(item2);

	                    var item3 = new ListItem()
	                    {
		                    ImageSource = new CustomStreamImageSource()
		                    {
			                    Key = images[i],
			                    Stream = (token) => Task.FromResult<Stream>(new MemoryStream(byteArray))
		                    },
		                    FileName = string.Format("image{0}.jpeg", i + 1),
	                    };
	                    Items.Add(item3);
                    }
                }
            }
        }

        public class CustomStreamImageSource : StreamImageSource
        {
            public string Key { get; set; }
        }

        public partial class ListItem : ObservableObject
        {
			[ObservableProperty]
			ImageSource imageSource;

			[ObservableProperty]
			string fileName;
        }
    }
}
