Fast & Furious Image Loading
==========================================

Library to load images quickly & easily on Xamarin.iOS, Xamarin.Android, Xamarin.Forms and Windows (WinRT, UWP).

| Xamarin iOS / Android | Xamarin Forms | Windows RT / UWP | Transformations |
|:----:|:----:|:----:|:----:|
| [Nuget](https://www.nuget.org/packages/Xamarin.FFImageLoading/) | [Nuget](https://www.nuget.org/packages/Xamarin.FFImageLoading.Forms/) | [Nuget](https://www.nuget.org/packages/FFImageLoading.Windows/) | [Nuget](https://www.nuget.org/packages/Xamarin.FFImageLoading.Transformations/) |

<a href="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_list.png"><img src="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_list.png" width="150"/></a> <a href="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_transformations.png"><img src="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_transformations.png" width="150"/></a> <a href="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_placeholders1.png"><img src="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_placeholders1.png" width="150"/></a> <a href="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_placeholders2.png"><img src="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/android_placeholders2.png" width="150"/></a>

**Xamarin.Forms Demo:** [link](https://github.com/molinch/FFImageLoading/tree/master/samples/ImageLoading.Forms.Sample), **Android Demo:** [link](https://github.com/molinch/FFImageLoading/tree/master/samples/ImageLoading.Sample), **Windows Demo:** [link](https://github.com/molinch/FFImageLoading/tree/master/samples/Simple.WinPhone.Sample)


## Features

- Xamarin.iOS (min iOS 7), Xamarin.Android (min Android 4), Xamarin.Forms and Windows (WinRT, UWP) support
- Configurable disk and memory caching
- Multiple image views using the same image source (url, path, resource) will use only one bitmap which is cached in memory (less memory usage)
- Deduplication of similar download/load requests
- Error and loading placeholders support
- Images can be automatically downsampled to specified size (less memory usage)
- WebP support
- Image loading Fade-In animations support
- Can retry image downloads (RetryCount, RetryDelay)
- On Android transparency is disabled by default (configurable). Saves 50% of memory
- Transformations support
  - BlurredTransformation
  - CircleTransformation, RoundedTransformation, CornersTransformation
  - ColorSpaceTransformation, GrayscaleTransformation, SepiaTransformation
  - FlipTransformation
  - Supports custom transformations (native platform `ITransformation` implementations)

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
// IMPORTANT!!! Initialization:
CachedImageRenderer.Init();
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

### API
[API usage is described here](docs/API_Usage.md)

### Android remarks
Unlike Picasso you cannot use FFImageLoading with standard ImageViews. Instead simply load your images into ImageViewAsync instances. Updating your code is very easy since ImageViewAsync inherits from ImageView.

By default, on Android, images are loaded without transparency channel. This allows saving 50% of memory since 1 pixel uses 2 bytes instead of 4 bytes in RGBA.
- This is overridable for all images using `ImageService.Initialize(loadWithTransparencyChannel:true)`
- Or, per image request, by explicitly setting `TaskParameter.TransparencyChannel(true or false)`

### Advanced usage
[Advanced usage is described here](docs/Advanced_Usage.md)
