using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sample
{
	public partial class PlaceholdersPageModel : ObservableObject
	{
		public PlaceholdersPageModel()
		{
		}

		[RelayCommand]
		public void LoadingPlaceholder()
		{
			ImageUrl = Helpers.GetRandomImageUrl();
		}

		[RelayCommand]
		public void ErrorPlaceholder()
		{
			ImageUrl = "http://notexisting.com/notexisting.png";
		}

		[ObservableProperty]
		string imageUrl;
	}
}
