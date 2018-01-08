// 171(c) Andrei Misiukevich
using System;
using Xamvvm;
using System.Windows.Input;
using Xamarin.Forms;

namespace FFImageLoading.Forms.Sample.Pages
{
    public class XmlSamplePageModel : BasePageModel
    {
        private ICommand _clickCommand;

        public int ImageHeight { get; set; } = 300;

        public string Image { get; set; } = "testimage2";

        public ICommand ClickCommand => _clickCommand ?? (_clickCommand = new Command(() =>
        {
            if(ImageHeight == 300)
            {
                ImageHeight = 100;
                Image = "testimage";
            }
            else
            {
                ImageHeight = 300;
                Image = "testimage2";
            }
        }));
    }
}
