using System;
using DLToolkit.PageFactory;

namespace FFImageLoading.Forms.Sample.ViewModels
{
	public class HomeViewModel : BaseViewModel
	{
		public HomeViewModel()
		{
			OpenSimpleExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<SimpleExampleViewModel>().PushPage());

			OpenListExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<ListExampleViewModel>().PushPage());

			OpenListTransformationsExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<ListTransformationExampleViewModel>().PushPage());

			OpenPlaceholdersExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<PlaceholdersExampleViewModel>().PushPage());

			OpenTransformationsExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<TransformationExampleViewModel>().PushPage());

			OpenDownsamplingExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<DownsamplingExampleViewModel>().PushPage());
		}

		public IPageFactoryCommand OpenSimpleExampleCommand { get; private set; }

		public IPageFactoryCommand OpenListExampleCommand { get; private set; }

		public IPageFactoryCommand OpenListTransformationsExampleCommand { get; private set; }

		public IPageFactoryCommand OpenPlaceholdersExampleCommand { get; private set; }

		public IPageFactoryCommand OpenTransformationsExampleCommand { get; private set; }

		public IPageFactoryCommand OpenDownsamplingExampleCommand { get; private set; }
	}
}

