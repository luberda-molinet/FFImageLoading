using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sample.Pages;
using Sample.Pages.Transformations;

namespace Sample
{
    public partial class MenuPageModel : ObservableObject
    {
        protected readonly INavigation Navigation;

        public MenuPageModel(INavigation navigation)
        {
            Navigation = navigation;

            var menuItems = new List<MenuItem>()
            {
                new MenuItem() {
                    Section = "Basic",
                    Title = "Basic example",
                    Command = new AsyncRelayCommand(async  _ =>
                    {
                        try
                        {
                            var p = new BasicPage();
                            await Navigation.PushAsync(p);

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    })
                },

                new MenuItem() {
                    Section = "Basic",
                    Title = "Placeholders examples",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new PlaceholdersPage()))
				},

                new MenuItem() {
                    Section = "Lists",
                    Title = "List example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new ListPage()))
				},

                new MenuItem() {
                    Section = "Lists",
                    Title = "List transformations example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new ListTransformationsPage()))
				},

                new MenuItem() {
                    Section = "Lists",
                    Title = "Heavy List example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new ListHeavyPage()))
				},

                new MenuItem() {
                    Section = "Lists",
                    Title = "ByteArray / custom cache key example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new ByteArrayListPage()))
				},

				new MenuItem() {
					Section = "Advanced",
					Title = "Exif tests",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new Pages.ExifPage()))
				},

				new MenuItem() {
                    Section = "Advanced",
                    Title = "Custom CacheKey example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new CustomKeyPage()))
				},

                new MenuItem() {
                    Section = "Advanced",
                    Title = "Stream from base64 data",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new StreamListPage()))
				},

                new MenuItem() {
                    Section = "Advanced",
                    Title = "Data url examples",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new DataUrlPage()))
				},

                new MenuItem() {
                    Section = "Advanced",
                    Title = "CachedImage sizing test",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new CachedImageSizingTestPage()))
				},

                //new MenuItem() {
                //	Section = "Advanced",
                //	Title = "Stream with custom cache key example",
                //	Command = new BaseCommand((param) =>
                //	{
                //		//TODO
                //	})
                //},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "ColorFillTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new ColorFillTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "CropTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new CropTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "RotateTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new RotateTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "CircleTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new CircleTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "RoundedTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new RoundedTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "CornersTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new CornersTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "GrayscaleTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new GrayscaleTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "BlurredTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new BlurredTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "SepiaTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new SepiaTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "ColorSpaceTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new ColorSpaceTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "TintTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new TintTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "FlipTransformation",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new FlipTransformationPage()))
				},

                new MenuItem() {
                    Section = "Transformations",
                    Title = "Multiple transformations example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new MultipleTransformationsPage()))
				},

                new MenuItem()
                {
                    Section = "Transformations",
                    Title = "Transformations selector example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new TransformationsSelectorPage()))

				},

                new MenuItem() {
                    Section = "File formats",
                    Title = "Simple SVG example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new SvgSamplePage()))
				},

                new MenuItem() {
                    Section = "File formats",
                    Title = "Heavy SVG List example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new SvgListHeavyPage()))
				},

                new MenuItem() {
                    Section = "File formats",
                    Title = "SVG replace map example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new SvgReplacePage()))
				},

                new MenuItem()
                {
                    Section = "File formats",
                    Title = "Simple Gif example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new SimpleGifPage()))

				},

                new MenuItem()
                {
                    Section = "File formats",
                    Title = "Simple WebP example",
					Command = new AsyncRelayCommand(_ =>
						Navigation.PushAsync(new SimpleWebpPage()))

				},
            };

            var sorted = menuItems
                .GroupBy(item => item.Section)
                .Select(itemGroup => new Grouping<string, MenuItem>(itemGroup.Key, itemGroup));

            Items = new ObservableCollection<Grouping<string, MenuItem>>(sorted);
        }

        public ObservableCollection<Grouping<string, MenuItem>> Items { get; set; }

        [ObservableProperty]
        MenuItem selectedItem;

        public void ItemSelected()
        {
            SelectedItem = null;
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



        public partial class MenuItem : ObservableObject
        {
			[ObservableProperty]
			string section;

			[ObservableProperty]
			string title;

			[ObservableProperty]
			string details;

            [ObservableProperty]
            ICommand command;
        }
    }
}
