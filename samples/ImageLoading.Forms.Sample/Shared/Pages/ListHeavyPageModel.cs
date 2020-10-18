using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Xamarin.Forms;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
    public class ListHeavyPageModel : BasePageModel
    {
        public ListHeavyPageModel()
        {
            ItemSelectedCommand = new BaseCommand<SelectedItemChangedEventArgs>((arg) =>
            {
                SelectedItem = null;
            });
        }

        public ListHeavyItem SelectedItem { get; set; }

        public ICommand ItemSelectedCommand { get; set; }

        public ObservableCollection<ListHeavyItem> Items { get; set; }

        public void Reload()
        {
            var list = new List<ListHeavyItem>();

            string[] images = new string[50];

            for (int i = 0; i < images.Length; i++)
            {
                images[i] = Helpers.GetImageUrl(i);
            }

            var howMuch = images.Length;
            var howManyTimes = 10;

            for (int j = 0; j < howManyTimes; j++)
			{
                for (int i = 0; i < howMuch; i++)
                {
                    var item = new ListHeavyItem()
                    {
                        Image1Url = images[i],
                        Image2Url = images[i],
                        Image3Url = images[i],
                        Image4Url = images[i],
                    };

                    list.Add(item);
                }
            }

            Items = new ObservableCollection<ListHeavyItem>(list);
        }

        
        public class ListHeavyItem : BaseModel
        {
            public string Image1Url { get; set; }

            public string Image2Url { get; set; }

            public string Image3Url { get; set; }

            public string Image4Url { get; set; }
        }
    }
}
