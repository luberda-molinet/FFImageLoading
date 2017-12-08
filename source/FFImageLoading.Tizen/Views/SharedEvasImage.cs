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
}
