#if !ANDROID && !WINDOWS && !IOS && !TIZEN && !MACCATALYST
using System;

namespace FFImageLoading.Mock
{
    public class MockBitmap
    {
        public MockBitmap()
        {
        }

        public Guid Id { get; } = Guid.NewGuid();
    }
}
#endif
