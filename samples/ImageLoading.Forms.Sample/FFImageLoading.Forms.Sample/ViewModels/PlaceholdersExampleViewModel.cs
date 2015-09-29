using System;
using DLToolkit.PageFactory;

namespace FFImageLoading.Forms.Sample.ViewModels
{
	public class PlaceholdersExampleViewModel : BaseViewModel
	{
		public PlaceholdersExampleViewModel()
		{
			RemoteLoadingPlaceholderExampleCommand = new PageFactoryCommand(() => {
				ErrorImagePath = null;
				LoadingImagePath = "http://res.cloudinary.com/dqeaiomo8/image/upload/v1443461222/loading_xcotss.png";
				ImagePath = GetRandomImageUrl();
			});

			LocalLoadingPlaceholderExampleCommand = new PageFactoryCommand(() => {
				ErrorImagePath = null;
				LoadingImagePath = "loading.png";
				ImagePath = GetRandomImageUrl();
			});

			LocalErrorPlaceholderExampleCommand = new PageFactoryCommand(() => {
				//LoadingImagePath = "loading.png";
				LoadingImagePath = null;
				ErrorImagePath = "error.png";
				ImagePath = "http://notexisting.com/notexisting.jpg";
			});

			RemoteErrorPlaceholderExampleCommand = new PageFactoryCommand(() => {
				//LoadingImagePath = "loading.png";
				LoadingImagePath = null;
				ErrorImagePath = "http://res.cloudinary.com/dqeaiomo8/image/upload/v1443461219/error_xxhxfn.png";
				ImagePath = "http://notexisting.com/notexisting.jpg";
			});
		}

		public string GetRandomImageUrl()
		{
			return string.Format("http://loremflickr.com/600/600/nature?filename={0}.jpg", 
				Guid.NewGuid().ToString("N"));
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

