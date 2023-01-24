
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample
{
    public partial class SimpleGifPageModel : ObservableObject
    {
        public void Reload()
        {
            ImageUrl = "resource://tenor.gif";
        }

        [ObservableProperty]
        string imageUrl;
    }
}
