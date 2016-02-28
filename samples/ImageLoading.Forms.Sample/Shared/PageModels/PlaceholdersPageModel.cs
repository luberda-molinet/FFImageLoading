using System;
using DLToolkit.PageFactory;
using System.Windows.Input;

namespace FFImageLoading.Forms.Sample.PageModels
{
    public class PlaceholdersPageModel : CommonPageModel
	{
		public PlaceholdersPageModel()
		{
			LocalLoadingCommand = new PageFactoryCommand(() => {
				ErrorImagePath = null;
				LoadingImagePath = "loading.png";
				ImagePath = GetRandomImageUrl(width: 200, height: 200);
			});

			RemoteLoadingCommand = new PageFactoryCommand(() => {
				ErrorImagePath = null;
				LoadingImagePath = "http://res.cloudinary.com/dqeaiomo8/image/upload/v1443461222/loading_xcotss.png";
				ImagePath = GetRandomImageUrl(width: 200, height: 200);
			});

			LocalErrorCommand = new PageFactoryCommand(() => {
				ErrorImagePath = "error.png";
				LoadingImagePath = null;
				ImagePath = "http://notexisting.com/notexisting.jpg";
			});

			RemoteErrorCommand = new PageFactoryCommand(() => {
				ErrorImagePath = "http://res.cloudinary.com/dqeaiomo8/image/upload/v1443461219/error_xxhxfn.png";
				LoadingImagePath = null;
				ImagePath = "http://notexisting.com/notexisting.jpg";
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

		public string ErrorImagePath 
        {
			get { return GetField<string>(); }
			set { SetField(value); }
		}

		public string LoadingImagePath 
        {
			get { return GetField<string>(); }
			set { SetField(value); }
		}

        public ICommand RemoteLoadingCommand
        {
            get { return GetField<ICommand>(); }
            set { SetField(value); }
        }

        public ICommand LocalLoadingCommand
        {
            get { return GetField<ICommand>(); }
            set { SetField(value); }
        }

        public ICommand RemoteErrorCommand
        {
            get { return GetField<ICommand>(); }
            set { SetField(value); }
        }

        public ICommand LocalErrorCommand
        {
            get { return GetField<ICommand>(); }
            set { SetField(value); }
        }
	}
}

