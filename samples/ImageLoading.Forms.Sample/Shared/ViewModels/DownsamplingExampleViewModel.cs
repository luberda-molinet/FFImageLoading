using System;
using DLToolkit.PageFactory;

namespace FFImageLoading.Forms.Sample.ViewModels
{
	public class DownsamplingExampleViewModel : BaseExampleViewModel
	{
		public DownsamplingExampleViewModel()
		{
			DownsampleHeight200ExampleCommand = new PageFactoryCommand(() => {
				DownsampleHeight = 200f;
				DownsampleWidth = 0f;
				ImagePath = GetRandomImageUrl(width: 800, height: 600);
			});

			DownsampleWidth200ExampleCommand = new PageFactoryCommand(() => {
				DownsampleHeight = 0f;
				DownsampleWidth = 200f;	
				ImagePath = GetRandomImageUrl(width: 800, height: 600);
			});
		}

		public string ImagePath {
			get { return GetField<string>(); }
			set { SetField(value); }
		}

		public double DownsampleWidth  {
			get { return GetField<double>(); }
			set { SetField(value); }
		}

		public double DownsampleHeight  {
			get { return GetField<double>(); }
			set { SetField(value); }
		}

		public IPageFactoryCommand DownsampleHeight200ExampleCommand { get; private set; }

		public IPageFactoryCommand DownsampleWidth200ExampleCommand { get; private set; }
	}
}

