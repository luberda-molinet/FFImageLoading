using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FFImageLoading.Svg.Maui;

namespace Sample
{
    public class SvgReplacePageModel : ObservableObject
    {
        private Random _random = new Random();

        public SvgReplacePageModel()
        {
        }

        public Dictionary<string, string> ReplaceMap { get; set; }

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

			ImageSource = SvgImageSource.FromResource("Sample.Resources.replace.svg",
				replaceStringMap: ReplaceMap);
		}

		public ImageSource ImageSource { get; set; }
    }
}
