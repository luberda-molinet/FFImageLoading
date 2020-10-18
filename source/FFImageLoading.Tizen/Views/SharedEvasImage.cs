using System.Reflection;
using ElmSharp;

namespace FFImageLoading.Views
{
    public class SharedEvasImage : EvasImage
    {
        object _lock = new object();
        int _refCount = 0;
        public SharedEvasImage(EvasObject parent) : base(parent)
        {
        }

        public void AddRef()
        {
            lock (_lock)
            {
                _refCount++;
            }
        }
        public void RemoveRef()
        {
            lock (_lock)
            {
                _refCount--;
                if (_refCount <= 0)
                {
                    this.DisposeOnMainThread();
                }
            }
        }
    }

    internal static class EvasImageDisposerEx
    {
        public static void DisposeOnMainThread(this SharedEvasImage image)
        {
            if (EcoreMainloop.IsMainThread)
            {
                image.Unrealize();
            }
            else
            {
                EcoreMainloop.Post(() => {
                    image.Unrealize();
                });
            }
        }
    }

    internal static class EvasImageEx
    {
        public static void SetScaleDown(this EvasImage evasImage, int scale)
        {
            var interop = typeof(EvasObject).Assembly.GetType("Interop");
            var elementary = interop?.GetNestedType("Elementary", BindingFlags.NonPublic | BindingFlags.Static) ?? null;
            var method = elementary?.GetMethod("evas_object_image_load_scale_down_set", BindingFlags.NonPublic | BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, new object[] { evasImage.RealHandle, scale });
            }
            else
            {
                System.Console.WriteLine("No API evas_object_image_load_scale_down_set");
            }
        }


        public static void Save(this EvasImage evasImage, string file, string key, string flags)
        {
            typeof(EvasImage)?.GetMethod("Save")?.Invoke(evasImage, new[] { file, key, flags });
        }
    }
}
