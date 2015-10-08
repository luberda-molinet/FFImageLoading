## Advanced usage

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