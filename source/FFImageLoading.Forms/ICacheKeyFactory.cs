using System;
using Xamarin.Forms;

namespace FFImageLoading.Forms
{
	public interface ICacheKeyFactory
	{
		string GetKey(ImageSource imageSource, object bindingContext);
	}
}

