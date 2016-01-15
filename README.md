# FFImageLoading - Fast & Furious Image Loading

Library to load images quickly & easily on Xamarin.iOS, Xamarin.Android, Xamarin.Forms and Windows (WinRT, UWP, Silverlight).

*Authors: Fabien Molinet, Daniel Luberda*

| Xamarin iOS / Android | Xamarin Forms | Windows RT / UWP | Transformations |
|:----:|:----:|:----:|:----:|
| [Nuget](https://www.nuget.org/packages/Xamarin.FFImageLoading/) | [Nuget](https://www.nuget.org/packages/Xamarin.FFImageLoading.Forms/) | [Nuget](https://www.nuget.org/packages/FFImageLoading.Windows/) | [Nuget](https://www.nuget.org/packages/Xamarin.FFImageLoading.Transformations/) |

<a href="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/ffimageloading_large.png"><img src="https://raw.githubusercontent.com/molinch/FFImageLoading/master/samples/Screenshots/ffimageloading.png"/></a>

**Xamarin.Forms Demo:** [link](https://github.com/molinch/FFImageLoading/tree/master/samples/ImageLoading.Forms.Sample), **Android Demo:** [link](https://github.com/molinch/FFImageLoading/tree/master/samples/ImageLoading.Sample), **Windows Demo:** [link](https://github.com/molinch/FFImageLoading/tree/master/samples/Simple.WinPhone.Sample)


## Features

- Xamarin.iOS (min iOS 7), Xamarin.Android (min Android 4), Xamarin.Forms and Windows (WinRT, UWP, Silverlight) support
- Configurable disk and memory caching
- Multiple image views using the same image source (url, path, resource) will use only one bitmap which is cached in memory (less memory usage)
- Deduplication of similar download/load requests. *(If 100 similar requests arrive at same time then one real loading will be performed while 99 others will wait).*
- Error and loading placeholders support
- Images can be automatically downsampled to specified size (less memory usage)
- Fluent API which is inspired by Picasso naming
- WebP support
- Image loading Fade-In animations support
- Can retry image downloads (RetryCount, RetryDelay)
- On Android transparency is disabled by default (configurable). Saves 50% of memory
- Transformations support
  - BlurredTransformation
  - CircleTransformation, RoundedTransformation, CornersTransformation, CropTransformation
  - ColorSpaceTransformation, GrayscaleTransformation, SepiaTransformation
  - FlipTransformation, RotateTransformation
  - Supports custom transformations (native platform `ITransformation` implementations)

## Documentation

- **General**
  - [Fluent Syntax](https://github.com/molinch/FFImageLoading/wiki/Fluent-Syntax)

- **Android, iOS, Windows Phone**
  - [Common API](https://github.com/molinch/FFImageLoading/wiki/API)
  - [Android Specificities](https://github.com/molinch/FFImageLoading/wiki/Android-API)
  - [iOS Specificities](https://github.com/molinch/FFImageLoading/wiki/iOS-API)
  - [Windows Specificities](https://github.com/molinch/FFImageLoading/wiki/Windows-API)
  - [Advanced usage](https://github.com/molinch/FFImageLoading/wiki/Advanced-Usage)

- **Xamarin.Forms**
  - [Xamarin.Forms API](https://github.com/molinch/FFImageLoading/wiki/Xamarin.Forms-API)
  - [Xamarin.Forms Advanced](https://github.com/molinch/FFImageLoading/wiki/Xamarin.Forms-Advanced)

- **Transformations**
  - [Transformations Guide](https://github.com/molinch/FFImageLoading/wiki/Transformations-Guide)
  - [Custom Transformations Guide](https://github.com/molinch/FFImageLoading/wiki/Custom-Transformations-Guide)
