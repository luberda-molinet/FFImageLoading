using System;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
	[PropertyChanged.ImplementPropertyChanged]
	public class SvgSamplePageModel : BasePageModel
	{
		public SvgSamplePageModel()
		{
		}

		public string Source { get; set; } = "sample.svg";
	}
}
