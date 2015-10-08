## API
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