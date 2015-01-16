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

The code has been released after a discussion on Xamarin forums.

###Warning
Some code could be refactored, improved, probably better written too. If you feel that way you can do a pull request.
Please remember that this has been developed for our needs. We never though of releasing it when writing it.

###Simple usage
Use the ImageViewAsync instead of the Android ImageView.

Then when you want to load the image from a file:
```C#
_imageView.SetFromFile(fullPathToImage);
```

Or from an URL. In this case the image is cached (by default 30 days but there is an optional TimeSpan so you can choose yours).
```C#
_imageView.SetFromUrl(urlToImage);
```

You can also have a callback when the image is loaded.
```C#
_imageView.SetFromUrl(urlToImage, () =>
{
  // your code here...
});
```

If you want to resample the image so it takes less memory then you can define your new width/height. Note: it will keep aspect ratio even if you give crazy values to width/height and it can only downscale.
```C#
_imageView.SetFromUrl(urlToImage, resampleWidth: 150); // you can only give one value since we keep aspect ratio
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

Then you just need to inherit from ImageAsyncView and add your custom SetFromxxx method.
