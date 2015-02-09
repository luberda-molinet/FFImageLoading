Fast & Furious Image Loading
==========================================

Xamarin library to load images quickly & easily on iOS and Android.

NuGet package is here: https://www.nuget.org/packages/Xam.Android.ImageLoading/

###Minimum OS version
The library works starting from Android 4 and iOS 7.

###History
We developed this library while working on an app that displays tons of pictures, very quickly, and that are mostly not reused.

We tried to use Picasso with C# bindings, we got good performances but many memory issues too. We then chose to go C# all the way: we believed it would be easier for us to debug and maintain. It is the case.

###Description
The library offers a Fluent API which is inspired by Picasso naming.

The library automatically deduplicates similar requests: if 100 similar requests arrive at same time then one real loading will be performed while 99 others will wait. When the 1st real read is done then the 99 waiters will get the image.

Both a memory cache and a disk cache are present.

Note: Unlike Picasso you cannot use FFImageLoading with standard ImageViews. Instead you should simply load your images into ImageViewAsync instances. It is very easy to update your code since ImageViewAsync inherits from ImageView.

###API
when you want to load the image from a file:
```C#
ImageService.LoadFile(fullPathToImage).Into(_imageView);
```

Or from an URL. In this case the image is cached (by default 30 days but there is an optional TimeSpan so you can choose yours).
```C#
ImageService.LoadUrl(urlToImage).Into(_imageView);
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

There is a third callback: when image loading process finished. Whatever the result this method will be called:
```C#
ImageService.LoadUrl(urlToImage)
.Finish(workScheduled =>
{
  // your code here...
})
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

###Advanced usage

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

###Custom loading logic
Customizing the loading logic is very easy: inherit from ImageLoaderTask and put your own logic.
