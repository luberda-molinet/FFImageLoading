using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
    
    public class SimpleGifPageModel : BasePageModel
    {
        public void Reload()
        {
            ImageUrl = "https://media.giphy.com/media/l0Hlyi4ZMJI9MpFUQ/giphy.gif";
        }

        public string ImageUrl { get; set; }
    }
}
