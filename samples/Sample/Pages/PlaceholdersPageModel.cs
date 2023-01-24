using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample
{
	public partial class PlaceholdersPageModel : ObservableObject
	{
		public PlaceholdersPageModel()
		{
		}

		public void LoadingPlaceholder()
		{
			ImageUrl = Helpers.GetRandomImageUrl();
		}

		public void ErrorPlaceholder()
		{
			ImageUrl = "http://notexisting.com/notexisting.png";
		}

		[ObservableProperty]
		string imageUrl;
	}
}
