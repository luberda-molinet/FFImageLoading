using Xamvvm;

namespace Sample
{
    
    public class SimpleGifPageModel : BasePageModel
    {
        public void Reload()
        {
            ImageUrl = "resource://tenor.gif";
        }

        public string ImageUrl { get; set; }
    }
}
