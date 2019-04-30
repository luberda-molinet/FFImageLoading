using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using FFImageLoading.Forms.Sample;
using Xamarin.Forms;
using Xamvvm;
using FFImageLoading.Forms.Sample.Pages;
using FFImageLoading.Forms.Sample.Pages.Transformations;

namespace FFImageLoading.Forms.Sample
{

    public class MenuPageModel : BasePageModel
    {
        public MenuPageModel()
        {
            ItemSelectedCommand = new BaseCommand<SelectedItemChangedEventArgs>((arg) =>
            {
                SelectedItem = null;
            });

            var menuItems = new List<MenuItem>()
            {
                new MenuItem() {
                    Section = "Basic",
                    Title = "Basic example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<BasicPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Basic",
                    Title = "Placeholders examples",
                    Command = BaseCommand.FromAction(async(param) =>
                    {
                        await this.PushPageFromCacheAsync<PlaceholdersPageModel>();
                    })
                },

                new MenuItem() {
                    Section = "Lists",
                    Title = "List example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<ListPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Lists",
                    Title = "List transformations example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<ListTransformationsPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Lists",
                    Title = "Heavy List example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<ListHeavyPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Lists",
                    Title = "ByteArray / custom cache key example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<ByteArrayListPageModel>(pm => pm.Reload());
                    })
                },

				new MenuItem() {
					Section = "Advanced",
					Title = "Exif tests",
					Command = new BaseCommand(async (param) =>
					{
						await this.PushPageFromCacheAsync<ExifPageModel>();
					})
				},

				new MenuItem() {
                    Section = "Advanced",
                    Title = "Custom CacheKey example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<CustomKeyPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Advanced",
                    Title = "Stream from base64 data",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<StreamListPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Advanced",
                    Title = "Data url examples",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<DataUrlPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Advanced",
                    Title = "CachedImage sizing test",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<CachedImageSizingTestPageModel>();
                    })
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
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<ColorFillTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "CropTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<CropTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "RotateTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<RotateTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "CircleTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<CircleTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "RoundedTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<RoundedTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "CornersTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<CornersTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "GrayscaleTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<GrayscaleTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "BlurredTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<BlurredTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "SepiaTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<SepiaTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "ColorSpaceTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<ColorSpaceTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "TintTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<TintTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "FlipTransformation",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<FlipTransformationPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "Transformations",
                    Title = "Multiple transformations example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<MultipleTransformationsPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem()
                {
                    Section = "Transformations",
                    Title = "Transformations selector example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<TransformationsSelectorPageModel>(pm => pm.Reload());
                    })

                },

                new MenuItem() {
                    Section = "File formats",
                    Title = "Simple SVG example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<SvgSamplePageModel>();
                    })
                },

                new MenuItem() {
                    Section = "File formats",
                    Title = "Heavy SVG List example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<SvgListHeavyPageModel>(pm => pm.Reload());
                    })
                },

                new MenuItem() {
                    Section = "File formats",
                    Title = "SVG replace map example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<SvgReplacePageModel>();
                    })
                },

                new MenuItem()
                {
                    Section = "File formats",
                    Title = "Simple Gif example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<SimpleGifPageModel>(pm => pm.Reload());
                    })

                },

                new MenuItem()
                {
                    Section = "File formats",
                    Title = "Simple WebP example",
                    Command = new BaseCommand(async (param) =>
                    {
                        await this.PushPageFromCacheAsync<SimpleWebpPageModel>(pm => pm.Reload());
                    })

                },
            };

            var sorted = menuItems
                .GroupBy(item => item.Section)
                .Select(itemGroup => new Grouping<string, MenuItem>(itemGroup.Key, itemGroup));

            Items = new ObservableCollection<Grouping<string, MenuItem>>(sorted);
        }

        public ObservableCollection<Grouping<string, MenuItem>> Items { get; set; }

        public MenuItem SelectedItem { get; set; }

        public ICommand ItemSelectedCommand { get; set; }

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


        public class MenuItem : BaseModel
        {
            public string Section { get; set; }

            public string Title { get; set; }

            public string Details { get; set; }

            public ICommand Command { get; set; }
        }
    }
}
