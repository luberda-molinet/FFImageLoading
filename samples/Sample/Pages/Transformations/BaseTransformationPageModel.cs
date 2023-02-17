using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sample
{
	
	public partial class BaseTransformationPageModel : ObservableObject
	{
		public BaseTransformationPageModel()
		{
		}

		[ObservableProperty]
		string imageUrl;

		[RelayCommand]
		public void LoadAnother()
			=> Reload();

		[RelayCommand]
		public void Reload()
		{
			ImageUrl = Helpers.GetRandomImageUrl();
		}
	}
}
