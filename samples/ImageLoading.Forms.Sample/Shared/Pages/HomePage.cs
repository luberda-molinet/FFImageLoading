using System;
using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.PageModels;
using System.Threading.Tasks;

namespace FFImageLoading.Forms.Sample.Pages
{
    public class HomePage : ContentPage, IBasePage<HomePageModel>
	{
		public HomePage()
		{
			Title = "FFImageLoading Sample";

			var menuListView = new ListView() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,	
				RowHeight = 60,
				ItemTemplate = new DataTemplate(() => {
					var cell = new TextCell();
					cell.SetBinding<Models.MenuItem>(TextCell.TextProperty, v => v.Title);
					cell.SetBinding<Models.MenuItem>(TextCell.DetailProperty, v => v.Detail);
					cell.SetBinding<Models.MenuItem>(TextCell.CommandProperty, v => v.Command);
					cell.SetBinding<Models.MenuItem>(TextCell.CommandParameterProperty, v => v.CommandParameter);
					return cell;
				}),
				IsGroupingEnabled = true,
				GroupDisplayBinding = new Binding("Key"),
			};

            if (Device.OS == TargetPlatform.Android || Device.OS == TargetPlatform.iOS)
			    menuListView.ItemSelected += (sender, e) => { menuListView.SelectedItem = null; };

			menuListView.SetBinding<HomePageModel>(ListView.ItemsSourceProperty, v => v.MenuItems);

			Content = menuListView;
		}
	}
}


