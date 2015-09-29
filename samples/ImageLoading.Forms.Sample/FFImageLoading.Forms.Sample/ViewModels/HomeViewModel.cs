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

			OpenPlaceholdersExampleCommand = new PageFactoryCommand(() => 
				PageFactory.GetMessagablePageFromCache<PlaceholdersExampleViewModel>().PushPage());
		}

		public IPageFactoryCommand OpenSimpleExampleCommand { get; private set; }

		public IPageFactoryCommand OpenListExampleCommand { get; private set; }

		public IPageFactoryCommand OpenPlaceholdersExampleCommand { get; private set; }
	}
}

