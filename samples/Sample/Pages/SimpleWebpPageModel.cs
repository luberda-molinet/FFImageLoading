using System;
using Xamvvm;

namespace Sample
{
    public class SimpleWebpPageModel : BasePageModel
    {
		public void Reload()
		{
			ImageUrl = "https://www.gstatic.com/webp/gallery/1.sm.webp";
		}

		public string ImageUrl { get; set; }
    }
}
