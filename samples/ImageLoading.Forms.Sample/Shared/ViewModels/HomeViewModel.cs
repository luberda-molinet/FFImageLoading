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
			OpenTransformationExampleCommand = new PageFactoryCommand<Type>((transformationType) => 
				PageFactory.GetMessagablePageFromCache<TransformationExampleViewModel>()
				.SendMessageToViewModel("LoadTransformation", this, transformationType)
				.PushPage());
			
			var menuItems = new List<MenuItem>() {
				
				new MenuItem() {
					Section = "Basic",
					Title = "Basic example",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<SimpleExampleViewModel>()
							.PushPage())
				},

				new MenuItem() {
					Section = "Basic",
					Title = "Basic XAML example",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<XamlSimpleExampleViewModel>()
							.PushPage())
				},

				new MenuItem() {
					Section = "Lists",
					Title = "List example",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<ListExampleViewModel>()
							.SendMessageToViewModel("Reload")
							.PushPage())
				},

				new MenuItem() {
					Section = "Lists",
					Title = "List transformations example",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<ListTransformationExampleViewModel>()
							.SendMessageToViewModel("Reload")
							.PushPage())
				},

				new MenuItem() {
					Section = "Lists",
					Title = "Heavy Grid List example",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<ListHeavyTestViewModel>()
							.SendMessageToViewModel("Reload")
							.PushPage())
				},

				new MenuItem() {
					Section = "Basic",
					Title = "Placeholders examples",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<PlaceholdersExampleViewModel>()
							.PushPage())
				},

				new MenuItem() {
					Section = "Basic",
					Title = "Downsampling examples",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<DownsamplingExampleViewModel>()
							.PushPage())
				},

				new MenuItem() {
					Section = "Advanced",
					Title = "Custom CacheKey example",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<CustomCacheKeyViewModel>()
							.SendMessageToViewModel("Reload")
							.PushPage())
				},

				new MenuItem() {
					Section = "Advanced",
					Title = "Stream with custom cache key example",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<StreamTestViewModel>()
						.PushPage())
				},

				new MenuItem() {
					Section = "Transformations",
					Title = "CropTransformation",
					Command = new PageFactoryCommand(() => 
						PageFactory.GetMessagablePageFromCache<CropTransformationViewModel>()
						.SendMessageToViewModel("Reload")
						.PushPage()),
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

		public IPageFactoryCommand OpenTransformationExampleCommand { get; private set; }

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
	}
}

