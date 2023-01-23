using System;

namespace FFImageLoading.Maui
{
    [Preserve(AllMembers = true)]
	public interface ICacheKeyFactory
	{
		string GetKey(ImageSource imageSource, object bindingContext);
	}
}

