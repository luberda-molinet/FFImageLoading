using System;
using DLToolkit.PageFactory;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using FFImageLoading.Forms.Sample.Models;
using FFImageLoading.Transformations;

namespace FFImageLoading.Forms.Sample.ViewModels
{
	public class HomeViewModel : BaseViewModel
	{
		public HomeViewModel()
		{
			OpenBasicExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<SimpleExampleViewModel>()
				.PushPage());

			OpenXamlBasicExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<XamlSimpleExampleViewModel>()
				.PushPage());

			OpenListExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<ListExampleViewModel>()
				.SendMessageToViewModel("Reload")
				.PushPage());

			OpenListTransformationsExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<ListTransformationExampleViewModel>()
				.SendMessageToViewModel("Reload")
				.PushPage());

			OpenHeavyListTransformationsExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<ListHeavyTestViewModel>()
				.SendMessageToViewModel("Reload")
				.PushPage());

			OpenPlaceholdersExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<PlaceholdersExampleViewModel>()
				.PushPage());

			OpenCropTransformationExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<CropTransformationViewModel>()
				.SendMessageToViewModel("Reload")
				.PushPage());

			OpenTransformationExampleCommand = new PageFactoryCommand<Type>((transformationType) => 
				PageFactory.GetMessagablePageFromCache<TransformationExampleViewModel>()
				.SendMessageToViewModel("LoadTransformation", this, transformationType)
				.PushPage());

			OpenDownsamplingExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<DownsamplingExampleViewModel>()
				.PushPage());

			var menuItems = new List<MenuItem>() {
				
				new MenuItem() {
					Section = "Basic",
					Title = "Basic example",
					Command = OpenBasicExampleCommand
				},

				new MenuItem() {
					Section = "Basic",
					Title = "Basic XAML example",
					Command = OpenXamlBasicExampleCommand
				},

				new MenuItem() {
					Section = "Lists",
					Title = "List example",
					Command = OpenListExampleCommand
				},

				new MenuItem() {
					Section = "Lists",
					Title = "List transformations example",
					Command = OpenListTransformationsExampleCommand
				},

				new MenuItem() {
					Section = "Lists",
					Title = "Heavy Grid List example",
					Command = OpenHeavyListTransformationsExampleCommand
				},

				new MenuItem() {
					Section = "Basic",
					Title = "Placeholders examples",
					Command = OpenPlaceholdersExampleCommand
				},

				new MenuItem() {
					Section = "Basic",
					Title = "Downsampling examples",
					Command = OpenDownsamplingExampleCommand
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "CropTransformation",
					Command = OpenCropTransformationExampleCommand,
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "RotateTransformation",
					Command = OpenTransformationExampleCommand,
					CommandParameter = typeof(RotateTransformation),
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "CircleTransformation",
					Command = OpenTransformationExampleCommand,
					CommandParameter = typeof(CircleTransformation),
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "RoundedTransformation",
					Command = OpenTransformationExampleCommand,
					CommandParameter = typeof(RoundedTransformation),
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "CornersTransformation",
					Command = OpenTransformationExampleCommand,
					CommandParameter = typeof(CornersTransformation),
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "GrayscaleTransformation",
					Command = OpenTransformationExampleCommand,
					CommandParameter = typeof(GrayscaleTransformation),
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "BlurredTransformation",
					Command = OpenTransformationExampleCommand,
					CommandParameter = typeof(BlurredTransformation),
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "SepiaTransformation",
					Command = OpenTransformationExampleCommand,
					CommandParameter = typeof(SepiaTransformation),
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "ColorSpaceTransformation",
					Command = OpenTransformationExampleCommand,
					CommandParameter = typeof(ColorSpaceTransformation),
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "FlipTransformation",
					Command = OpenTransformationExampleCommand,
					CommandParameter = typeof(FlipTransformation),
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "Multiple transformations example",
					Command = OpenTransformationExampleCommand,
					CommandParameter = null,
				},
			};

			var sorted = menuItems
				.OrderBy(item => item.Section)
				.ThenBy(item => item.Title)
				.GroupBy(item => item.Section)
				.Select(itemGroup => new Grouping<string, MenuItem>(itemGroup.Key, itemGroup));

			MenuItems = new ObservableCollection<Grouping<string, MenuItem>>(sorted);
		}

		public ObservableCollection<Grouping<string, MenuItem>> MenuItems
		{
			get { return GetField<ObservableCollection<Grouping<string, MenuItem>>>(); }
			set { SetField(value); }
		}

		public class Grouping<K, T> : ObservableCollection<T>
		{
			public K Key { get; private set; }

			public Grouping(K key, IEnumerable<T> items)
			{
				Key = key;
				foreach (var item in items)
					this.Items.Add(item);
			}
		}

		public IPageFactoryCommand OpenBasicExampleCommand { get; private set; }

		public IPageFactoryCommand OpenXamlBasicExampleCommand { get; private set; }

		public IPageFactoryCommand OpenListExampleCommand { get; private set; }

		public IPageFactoryCommand OpenListTransformationsExampleCommand { get; private set; }

		public IPageFactoryCommand OpenHeavyListTransformationsExampleCommand { get; private set; }

		public IPageFactoryCommand OpenPlaceholdersExampleCommand { get; private set; }

		public IPageFactoryCommand OpenCropTransformationExampleCommand { get; private set; }

		public IPageFactoryCommand OpenTransformationExampleCommand { get; private set; }

		public IPageFactoryCommand OpenDownsamplingExampleCommand { get; private set; }


	}
}

