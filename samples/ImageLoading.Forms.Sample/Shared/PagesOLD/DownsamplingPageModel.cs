using System;
using DLToolkit.PageFactory;
using System.Windows.Input;

namespace FFImageLoading.Forms.Sample.PageModels
{
    public class DownsamplingPageModel : CommonPageModel
	{
		public DownsamplingPageModel()
		{
			DownsampleHeight200Command = new PageFactoryCommand(() => {
				DownsampleHeight = 200f;
				DownsampleWidth = 0f;
				ImagePath = GetRandomImageUrl(width: 1000, height: 500);
			});

			DownsampleWidth200Command = new PageFactoryCommand(() => {
				DownsampleHeight = 0f;
				DownsampleWidth = 200f;	
				ImagePath = GetRandomImageUrl(width: 1000, height: 500);
			});
		}

        public void FreeResources()
        {
            ImagePath = null;
        }

		public string ImagePath 
        {
			get { return GetField<string>(); }
			set { SetField(value); }
		}

		public double DownsampleWidth  
        {
			get { return GetField<double>(); }
			set { SetField(value); }
		}

		public double DownsampleHeight  
        {
			get { return GetField<double>(); }
			set { SetField(value); }
		}

		public ICommand DownsampleHeight200Command
        {
            get { return GetField<ICommand>(); }
            set { SetField(value); }
        }

		public ICommand DownsampleWidth200Command
        {
            get { return GetField<ICommand>(); }
            set { SetField(value); }
        }
	}
}

