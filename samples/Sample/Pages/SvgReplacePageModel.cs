using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFImageLoading.Svg.Maui;

namespace Sample
{
    public partial class SvgReplacePageModel : ObservableObject
    {
        private Random _random = new Random();

        public SvgReplacePageModel()
        {
        }

        public Dictionary<string, string> ReplaceMap { get; set; }

		[RelayCommand]
        public void Replace()
        {
			var r = _random.Next(256);
			var g = _random.Next(256);
			var b = _random.Next(256);

			ReplaceMap = new Dictionary<string, string>()
				{
					{ "#TEXT", Guid.NewGuid().ToString() },
					{ "#FILLCOLOR", $"#{r:X2}{g:X2}{b:X2}" }
				};

			var src = SvgImageSource.FromResource("Sample.replace.svg",
				replaceStringMap: ReplaceMap);

			ImageSource = src;
		}

		[ObservableProperty]
		ImageSource imageSource;
    }
}
