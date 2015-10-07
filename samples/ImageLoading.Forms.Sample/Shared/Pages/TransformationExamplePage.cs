using System;

using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.ViewModels;
using FFImageLoading.Forms.Sample.Models;
using System.Collections.Generic;
using FFImageLoading.Work;
using FFImageLoading.Transformations;

namespace FFImageLoading.Forms.Sample.Pages
{
	public class TransformationExamplePage : PFContentPage<TransformationExampleViewModel>
	{
		public TransformationExamplePage()
		{
			Title = "Transformations Demo";

			var cachedImage = new CachedImage() {
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				WidthRequest = 200,
				HeightRequest = 200,
				CacheDuration = TimeSpan.FromDays(30),
				DownsampleHeight = 200,
				RetryCount = 0,
				RetryDelay = 250,
				TransparencyEnabled = false,
			};

			cachedImage.SetBinding<TransformationExampleViewModel>(CachedImage.TransformationsProperty, v => v.Transformations);
			cachedImage.SetBinding<TransformationExampleViewModel>(CachedImage.LoadingPlaceholderProperty, v => v.LoadingImagePath);
			cachedImage.SetBinding<TransformationExampleViewModel>(CachedImage.ErrorPlaceholderProperty, v => v.ErrorImagePath);
			cachedImage.SetBinding<TransformationExampleViewModel>(CachedImage.SourceProperty, v => v.ImagePath);

			var button1 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Blurred Transformation Example",
				Command = ViewModel.BlurredTransformationExampleCommand
			};

			var button2 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Circle Transformation Example",
				Command = ViewModel.CircleTransformationExampleCommand
			};

			var button3 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "ColorSpace Transformation Example",
				Command = ViewModel.ColorSpaceTransformationExampleCommand
			};

			var button4 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Grayscale Transformation Example",
				Command = ViewModel.GrayscaleTransformationExampleCommand
			};

			var button5 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Rounded Transformation Example",
				Command = ViewModel.RoundedTransformationExampleCommand
			};

			var button6 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Sepia Transformation Example",
				Command = ViewModel.SepiaTransformationExampleCommand
			};

			var button7 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Multiple effects Transformation Example",
				Command = ViewModel.MultipleTransformationExampleCommand
			};

			var imagePath = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				FontSize = 9,
			};
			imagePath.SetBinding<TransformationExampleViewModel>(Label.TextProperty, v => v.ImagePath);

			Content = new ScrollView() {
				Content = new StackLayout { 
					Children = {
						imagePath,
						cachedImage,
						button1, 
						button2, 
						button3, 
						button4,
						button5,
						button6,
						button7,
					}
				}
			};
		}
	}
}


