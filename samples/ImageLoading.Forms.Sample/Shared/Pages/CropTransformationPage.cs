using System;

using Xamarin.Forms;
using FFImageLoading.Forms.Sample.ViewModels;
using DLToolkit.PageFactory;

namespace FFImageLoading.Forms.Sample.Pages
{
	public class CropTransformationPage : PFContentPage<CropTransformationViewModel>
	{
		public CropTransformationPage()
		{
			Content = new StackLayout { 
				Children = {
					new Label { Text = "Hello ContentPage" }
				}
			};
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			ViewModel.ImagePath = null;
		}
	}
}


