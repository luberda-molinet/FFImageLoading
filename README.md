# FFImageLoading.Compat - Fast & Furious Image Loading for .NET MAUI 


Forked from the amazingly popular [original FFImageLoading Library](https://github.com/luberda-molinet/FFImageLoading), this *Compat* version aims to ease your migration from Xamarin.Forms to .NET MAUI with a compatible implementation to get you up and running without rewriting the parts of your app that relied on the original library.

*Thanks to the Original Authors: Daniel Luberda, Fabien Molinet.*

## Usage

1. Install NuGet(s)
2. Add `.UseFFImageLoading()` to your MAUI app builder.


## Features

- .NET MAUI (iOS, MacCatalyst, Android, Windows) support
- Configurable disk and memory caching
- Multiple image views using the same image source (url, path, resource) will use only one bitmap which is cached in memory (less memory usage)
- Deduplication of similar download/load requests. *(If 100 similar requests arrive at same time then one real loading will be performed while 99 others will wait).*
- Error and loading placeholders support
- Images can be automatically downsampled to specified size (less memory usage)
- Fluent API which is inspired by Picasso naming
- SVG / WebP / GIF support
- Image loading Fade-In animations support
- Can retry image downloads (RetryCount, RetryDelay)
- Android bitmap optimization. Saves 50% of memory by trying not to use transparency channel when possible.
- Transformations support
  - BlurredTransformation
  - CircleTransformation, RoundedTransformation, CornersTransformation, CropTransformation
  - ColorSpaceTransformation, GrayscaleTransformation, SepiaTransformation, TintTransformation
  - FlipTransformation, RotateTransformation
  - Supports custom transformations (native platform `ITransformation` implementations)

## Original Library Documentation

https://github.com/luberda-molinet/FFImageLoading/wiki

