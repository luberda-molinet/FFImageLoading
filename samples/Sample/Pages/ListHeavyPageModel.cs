using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sample
{
    public partial class ListHeavyPageModel : ObservableObject
    {
        public ListHeavyPageModel()
        {
        }

		[ObservableProperty]
		ListHeavyItem selectedItem;

		[RelayCommand]
		public void ItemSelected() => SelectedItem = null;

        public ObservableCollection<ListHeavyItem> Items { get; set; } = new();

        public void Reload()
        {
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

                    Items.Add(item);
                }
            }

        }

        
        public partial class ListHeavyItem : ObservableObject
        {
			[ObservableProperty]
			string image1Url;

			[ObservableProperty]
			string image2Url;

			[ObservableProperty]
			string image3Url;

			[ObservableProperty]
			string image4Url;
        }
    }
}
