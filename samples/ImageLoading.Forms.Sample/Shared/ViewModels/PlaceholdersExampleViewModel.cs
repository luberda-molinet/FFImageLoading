using System;
using DLToolkit.PageFactory;

namespace FFImageLoading.Forms.Sample.ViewModels
{
	public class PlaceholdersExampleViewModel : BaseExampleViewModel
	{
		public PlaceholdersExampleViewModel()
		{
			LocalLoadingPlaceholderExampleCommand = new PageFactoryCommand(() => {
				ErrorImagePath = null;
				LoadingImagePath = "loading.png";
				ImagePath = GetRandomImageUrl(width: 200, height: 200);
			});

			RemoteLoadingPlaceholderExampleCommand = new PageFactoryCommand(() => {
				ErrorImagePath = null;
				LoadingImagePath = "http://res.cloudinary.com/dqeaiomo8/image/upload/v1443461222/loading_xcotss.png";
				ImagePath = GetRandomImageUrl(width: 200, height: 200);
			});

			LocalErrorPlaceholderExampleCommand = new PageFactoryCommand(() => {
				ErrorImagePath = "error.png";
				LoadingImagePath = null;
				ImagePath = "http://notexisting.com/notexisting.jpg";
			});

			RemoteErrorPlaceholderExampleCommand = new PageFactoryCommand(() => {
				ErrorImagePath = "http://res.cloudinary.com/dqeaiomo8/image/upload/v1443461219/error_xxhxfn.png";
				LoadingImagePath = null;
				ImagePath = "http://notexisting.com/notexisting.jpg";
			}); 
		}

		public string ImagePath {
			get { return GetField<string>(); }
			set { SetField(value); }
		}

		public string ErrorImagePath {
			get { return GetField<string>(); }
			set { SetField(value); }
		}

		public string LoadingImagePath {
			get { return GetField<string>(); }
			set { SetField(value); }
		}

		public IPageFactoryCommand RemoteLoadingPlaceholderExampleCommand { get; private set; }

		public IPageFactoryCommand LocalLoadingPlaceholderExampleCommand { get; private set; }

		public IPageFactoryCommand RemoteErrorPlaceholderExampleCommand { get; private set; }

		public IPageFactoryCommand LocalErrorPlaceholderExampleCommand { get; private set; }
	}
}

