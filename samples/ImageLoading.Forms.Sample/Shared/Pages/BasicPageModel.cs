using System;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
	[PropertyChanged.ImplementPropertyChanged]
	public class BasicPageModel : BasePageModel
	{
		public void Reload()
		{
			ImageUrl = Helpers.GetRandomImageUrl();
		}

		public string ImageUrl { get; set; }
	}
}
