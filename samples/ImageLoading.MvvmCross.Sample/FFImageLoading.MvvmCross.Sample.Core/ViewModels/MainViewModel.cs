using System;
using System.Collections.Generic;

namespace FFImageLoading.MvvmCross.Sample.Core
{
    public class MainViewModel : BaseViewModel
    {

        private List<Image> _images;
        public List<Image> Images
        {
            get { return _images; }
            set { SetProperty(ref _images, value); }
        }

        public override void Start()
        {
            Images = new List<Image>();
            for (int i = 0; i < 999; i++)
            {
                Images.Add(new Image($"https://dummyimage.com/{512 + i}/" + i));
            }
        }
    }
}
