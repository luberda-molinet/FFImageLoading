using System;
using Xamarin.Forms;

namespace FFImageLoading.Forms
{
    [Preserve(AllMembers = true)]
	public interface ICacheKeyFactory
	{
		string GetKey(ImageSource imageSource, object bindingContext);
	}
}

