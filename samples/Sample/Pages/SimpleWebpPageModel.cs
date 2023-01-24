using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample
{
    public partial class SimpleWebpPageModel : ObservableObject
    {
		public void Reload()
		{
			ImageUrl = "https://www.gstatic.com/webp/gallery/1.sm.webp";
		}

		[ObservableProperty]
		string imageUrl;
    }
}
