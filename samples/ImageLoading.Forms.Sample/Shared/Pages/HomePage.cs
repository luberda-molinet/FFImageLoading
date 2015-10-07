using System;

using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.ViewModels;

namespace FFImageLoading.Forms.Sample.Pages
{
	public class HomePage : PFContentPage<HomeViewModel>
	{
		public HomePage()
		{
			Title = "FFImageLoading Sample";

			var simpleMenu = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Simple Example",
				HeightRequest = 80,
				Command = ViewModel.OpenSimpleExampleCommand
			};

			var listMenu = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "List Example",
				HeightRequest = 80,
				Command = ViewModel.OpenListExampleCommand
			};

			var listTransformationsMenu = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "List Transformations Example",
				HeightRequest = 80,
				Command = ViewModel.OpenListTransformationsExampleCommand
			};

			var placeholdersMenu = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Placeholders Example",
				HeightRequest = 80,
				Command = ViewModel.OpenPlaceholdersExampleCommand
			};

			var transformationsMenu = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Transformations Example",
				HeightRequest = 80,
				Command = ViewModel.OpenTransformationsExampleCommand
			};

			Content = new ScrollView() {
				Content = new StackLayout { 
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand,
					Children = {
						simpleMenu,
						listMenu,
						listTransformationsMenu,
						placeholdersMenu,
						transformationsMenu
					}
				}
			};
		}
	}
}


