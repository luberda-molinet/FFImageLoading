using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sample
{
	public class PlaceholdersPageModel : ObservableObject
	{
		public PlaceholdersPageModel()
		{
			LoadingPlaceholderCommand = new BaseCommand((arg) =>
			{
				ImageUrl = Helpers.GetRandomImageUrl();
			});

			ErrorPlaceholderCommand = new BaseCommand((arg) =>
			{
				ImageUrl = "http://notexisting.com/notexisting.png";
			});
		}

		public ICommand LoadingPlaceholderCommand { get; set; }

		public ICommand ErrorPlaceholderCommand { get; set; }

		public string ImageUrl { get; set; }
	}
}
