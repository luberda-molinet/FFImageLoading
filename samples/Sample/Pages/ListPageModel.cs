using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample
{

	public partial class ListPageModel : ObservableObject
	{
		public ListPageModel()
		{
		}

		[ObservableProperty]
		ListItem selectedItem;

		public void ItemSelected(){

			SelectedItem = null;
		}

        [ObservableProperty]
        List<ListItem> items;


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

            Items = new List<ListItem>(list);
        }


	
        public partial class ListItem : ObservableObject
        {
			[ObservableProperty]
			string imageUrl;

			[ObservableProperty]
			string fileName;
        }
    }
}
