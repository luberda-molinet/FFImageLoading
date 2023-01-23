using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample
{
    
    public partial class BasicPageModel : ObservableObject
    {
        public void Reload()
        {
            // ImageUrl = Helpers.GetRandomImageUrl();
            ImageUrl = @"https://raw.githubusercontent.com/recurser/exif-orientation-examples/master/Landscape_3.jpg";
        }

		[ObservableProperty]
		string imageUrl;
    }
}
