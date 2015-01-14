Xam.Android.ImageLoading
==========================================

Xamarin library to load images quickly & easily on Android.

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
Use the SimpleImageViewAsync instead of the Android ImageView.

Then when you want to load the image:
```C#
_thumbnailImage.SetImage(fullPathToImage);
```

You can also have a callback when the image is loaded.
```C#
_thumbnailImage.SetImage(fullPathToImage, () =>
{
  // your code here...
});
```

If you want to stop loading requests when you leave your activity is no longer used/paused:
```C#
protected override void OnPause()
{
	base.OnPause();
	ImageWorker.SetExitTaskEarly(true);
}

protected override void OnResume()
{
	base.OnResume();
	ImageWorker.SetExitTaskEarly(false);
}
```

If you want to load many images in a scrollable list view or horizontal list view and you have performance issues while you fling. In our app with more than 1000 items in an horizontal list view it was the case. To solve it we used:
```C#

_myListView.ScrollStateChanged += (object sender, ScrollStateChangedEventArgs scrollArgs) => {
  switch (scrollArgs.ScrollState)
  {
    case ScrollState.Fling:
      ImageWorker.SetPauseWork(true); // all image loading requests will be silently canceled
      break;
    case ScrollState.Idle:
      ImageWorker.SetPauseWork(false); // loading requests are allowed again
      
      // Here you should have your custom method that forces redrawing visible list items
      _myListView.ForcePdfThumbnailsRedraw();
      break;
  }
};
```

###Advanced usage
As the name suggests it SimpleImageViewAsync is simple: it loads images from a full path. But you can customize the image loading logic very easily.

To do so you just need to inherit from ImageWorkerBase and ImageViewAsyncBase. You can look at ImageWorker and SimpleImageViewAsync they are both subclasses.
