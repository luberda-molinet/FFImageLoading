using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Xamarin.Forms;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
    
    public class ListPageModel : BasePageModel
    {
        public ListPageModel()
        {
            ItemSelectedCommand = new BaseCommand<SelectedItemChangedEventArgs>((arg) =>
            {
                SelectedItem = null;
            });
        }

        public ListItem SelectedItem { get; set; }

        public ICommand ItemSelectedCommand { get; set; }

        public ObservableCollection<ListItem> Items { get; set; }

        public void Reload()
        {
            var list = new List<ListItem>();

            var images = new string[20];

            for (int i = 0; i < images.Length; i++)
            {
                images[i] = Helpers.GetImageUrl(i, 320, 240);
            }

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i < images.Length; i++)
                {
                    var item1 = new ListItem()
                    {
                        ImageUrl = images[i],
                        FileName = string.Format("image{0}.jpeg", i + 1),
                    };
                    list.Add(item1);

                    var item2 = new ListItem()
                    {
                        ImageUrl = images[i],
                        FileName = string.Format("image{0}.jpeg", i + 1),
                    };
                    list.Add(item2);

                    var item3 = new ListItem()
                    {
                        ImageUrl = images[i],
                        FileName = string.Format("image{0}.jpeg", i + 1),
                    };
                    list.Add(item3);
                }
            }

            Items = new ObservableCollection<ListItem>(list);
        }

        
        public class ListItem : BaseModel
        {
            public string ImageUrl { get; set; }

            public string FileName { get; set; }
        }
    }
}
