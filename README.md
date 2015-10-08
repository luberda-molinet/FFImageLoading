Fast & Furious Image Loading
==========================================

Xamarin library to load images quickly & easily on Xamarin.iOS, Xamarin.Android and Xamarin.Forms.

**iOS / Android NuGet package:** https://www.nuget.org/packages/Xamarin.FFImageLoading/

**Xamarin.Forms NuGet package:** https://www.nuget.org/packages/Xamarin.FFImageLoading.Forms/

<a href="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_list.png"><img src="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_list.png" width="150"/></a> <a href="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_transformations.png"><img src="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_transformations.png" width="150"/></a> <a href="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_placeholders1.png"><img src="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_placeholders1.png" width="150"/></a> <a href="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_placeholders2.png"><img src="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_placeholders2.png" width="150"/></a>

**Xamarin.Forms Demo:** [link](https://github.com/molinch/FFImageLoading/tree/master/samples/ImageLoading.Forms.Sample) and **Android Demo:** [link](https://github.com/molinch/FFImageLoading/tree/master/samples/ImageLoading.Sample)

## Features

- Xamarin.iOS, Xamarin.Android, Xamarin.Forms support (PCL compatible)
- Configurable disk and memory caching
- Deduplication of similar download/load requests
- Error and loading placeholders support
- WebP support
- Image loading Fade-In animations support
- Can retry image downloads (RetryCount, RetryDelay)
- On Android transparency is disabled by default (configurable). Saves 50% of memory
- Transformations support
  - BlurredTransformation
  - CircleTransformation
  - ColorSpaceTransformation
  - GrayscaleTransformation
  - RoundedTransformation
  - SepiaTransformation
  - Supports custom transformations (native platform `ITransformation` implementations)

### Minimum OS version
The library works starting from Android 4 and iOS 7.

### History
We developed this library while working on an app that displays tons of pictures, very quickly, and that are mostly not reused. We tried to use Picasso with C# bindings, we got good performances but many memory issues too. We then chose to go C# all the way: we believed it would be easier for us to debug and maintain. It is the case.

### Description
The library offers a Fluent API which is inspired by Picasso naming.

The library automatically deduplicates similar requests: if 100 similar requests arrive at same time then one real loading will be performed while 99 others will wait. When the 1st real read is done then the 99 waiters will get the image.

Both a memory cache and a disk cache are present.

WebP is supported on both iOS and Android. Bindings have been done for iOS, ie: https://github.com/molinch/WebP.Touch, which are then included as a Nuget dependency. As long as your file ends with .webp it will be handled by the lib.

### Xamarin.Forms
The Xamarin Forms NuGet package ships FFImageLoading for iOS and Android. Thanks to Daniel Luberda for his help here.

```C#
// Initialization:
FFImageLoading.Forms.Droid.CachedImageRenderer.Init();
// or
FFImageLoading.Forms.Touch.CachedImageRenderer.Init();
```

```C#
// Xamarin.Forms example:
var cachedImage = new CachedImage() {
	HorizontalOptions = LayoutOptions.Center,
	VerticalOptions = LayoutOptions.Center,
	WidthRequest = 300,
	HeightRequest = 300,
	CacheDuration = TimeSpan.FromDays(30),
	DownsampleHeight = 300,
	RetryCount = 3,
	RetryDelay = 250,
	TransparencyEnabled = false,
	// Shown after loading error occurs:
	ErrorPlaceholder = "http://yourcompany.com/error.jpg",
	// Shown before targe image is loaded:
	LoadingPlaceholder = "loading.png",
	// Target Image:
	Source = "http://yourcompany.com/image.jpg",
};
```

### Android remarks
Unlike Picasso you cannot use FFImageLoading with standard ImageViews. Instead simply load your images into ImageViewAsync instances. Updating your code is very easy since ImageViewAsync inherits from ImageView.

By default, on Android, images are loaded without transparency channel. This allows saving 50% of memory since 1 pixel uses 2 bytes instead of 4 bytes in RGBA.
- This is overridable for all images using `ImageService.Initialize(loadWithTransparencyChannel:true)`
- Or, per image request, by explicitly setting `TaskParameter.TransparencyChannel(true or false)`


### API
when you want to load the image from a file:
```C#
ImageService.LoadFile(fullPathToImage).Into(_imageView);
```

Or from an URL. In this case the image is cached (by default 30 days but there is an optional TimeSpan so you can choose yours).
```C#
ImageService.LoadUrl(urlToImage).Into(_imageView);
```

Or from your assets/bundled data (typically on Android)
```C#
ImageService.LoadFileFromApplicationBundle(relativePathToImage).Into(_imageView);
```

You can also have callbacks when the image is succesfully loaded or when there was errors:
```C#
ImageService.LoadUrl(urlToImage)
.Success(() =>
{
  // your code here...
})
.Error(exception =>
{
  // your code here...
})
.Into(_imageView);
```
Note: this callback is also available with width and height. In this case the callback is an Action<int, int>.

There is a third callback: when image loading process finished. Whatever the result this method will be called:
```C#
ImageService.LoadUrl(urlToImage)
.Finish(workScheduled =>
{
  // your code here...
})
.Into(_imageView);
```

It is possible to define placeholders while image is loading or when an error occured
```C#
ImageService.LoadUrl(urlToImage)
.LoadingPlaceholder("loading.png") // by default placeholders load from file
.ErrorPlaceholder("http://mydomain.com/error.png", ImageSource.Url) // but they can also load from a URL
.Into(_imageView);
```

If your download failed, or something wrong happened you can automatically retry. Here if loading from the URL failed then we will try 3 more times with a 200ms interval between each trial.
```C#
ImageService.LoadUrl(urlToImage)
.Retry(3, 200)
.Into(_imageView);
```

If you want to downsample the image so it takes less memory then you can define your new width/height. Note: it will keep aspect ratio even if you give crazy values to width/height and it can only downscale.
```C#
// you can only give one value since we keep aspect ratio
ImageService.LoadUrl(urlToImage).DownSample(width: 150).Into(_imageView);
```

Transformations are possible too:
```C#
ImageService.LoadUrl(urlToImage)
.Transform(new CropCircleTransformation())
.Transform(new GrayscaleTransformation())
.Into(imgDisplay);
```
Original image, prior to transformation, is cached to disk. Transformed image is cached in memory.
If the same image, with same transformations is requested then it will be loaded from memory.
For more information about transformations open our sample project.

### Advanced usage

If you don't want to use the Retry() functionality but have your custom error handling/retry logic (for example: because you use Polly). Then you can use IntoAsync() instead of Into()
```C#
try {
	// IntoAsync will throw exceptions
	await ImageService.LoadUrl(urlToImage).IntoAsync(_imageView);
} catch (Exception ex) {
	// do whatever you want...
}
```

If you want to stop pending loading requests. For example when your activity gets paused/resumed:
```C#
protected override void OnPause()
{
	base.OnPause();
	ImageService.SetExitTasksEarly(true);
}

protected override void OnResume()
{
	base.OnResume();
	ImageService.SetExitTasksEarly(false);
}
```

If you want to load many images in a scrollable list view or horizontal list view and you have performance issues while you fling. In our app with more than 1000 items in an horizontal list view it was the case. To solve it we used:
```C#

_myListView.ScrollStateChanged += (object sender, ScrollStateChangedEventArgs scrollArgs) => {
  switch (scrollArgs.ScrollState)
  {
    case ScrollState.Fling:
      ImageService.SetPauseWork(true); // all image loading requests will be silently canceled
      break;
    case ScrollState.Idle:
      ImageService.SetPauseWork(false); // loading requests are allowed again
      
      // Here you should have your custom method that forces redrawing visible list items
      _myListView.ForcePdfThumbnailsRedraw();
      break;
  }
};
```

### Custom loading logic
Customizing the loading logic is very easy: inherit from ImageLoaderTask and put your own logic.
