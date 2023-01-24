
#if ANDROID
global using TImageContainer = FFImageLoading.Drawables.SelfDisposingBitmapDrawable;
#elif IOS || MACCATALYST
global using TImageContainer = UIKit.UIImage;
#elif TIZEN
global using TImageContainer = object;
#elif WINDOWS
global using TImageContainer = Microsoft.UI.Xaml.Media.Imaging.BitmapSource;
#else
global using TImageContainer = object;
#endif
