Xam.Android.ImageLoading
==========================================

Xamarin library to load images quickly & easily on Android.

NuGet package is here: https://www.nuget.org/packages/Xam.Android.ImageLoading/

###Minimum Android version
The library works starting from Android 4.1.

###History
We developed this library while working on an app that displays tons of pictures, very quickly, and that are mostly not reused.

First we tried to use Picasso with C# bindings, we got good performances but after 15-20 minutes of usage our app was crashing due to memory issues: we saw some people having similar issues with Picasso and chosed to go C# all the way.
We believed it would be easier for us to debug and maintain. For us, it is the case.

The code has been released after a discussion on Xamarin forums. It changed quite a lot since it was released.

###Warning
Some code could be refactored, improved, probably better written too. If you feel that way you can do a pull request.
Please remember that this has been developed for our needs. We never though of releasing it when writing it.

###Description
The library offers a Fluent API which is inspired by Picasso naming.

Contrary to Picasso you cannot use ImageLoading with standard ImageViews. Instead you should load your images into  ImageViewAsync instances. It should be very easy to update your code since ImageViewAsync inherits from ImageView.

The library automatically deduplicates similar requests: if 100 similar requests arrive at same time then one real loading will be performed while 99 others will wait. When the 1st real read is done then the 99 waiters will get the image.

Both a memory cache and a disk cache are present.

###Examples
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
Customizing the loading logic is very easy: you should inherit from ImageLoaderTask and there put your own logic.
