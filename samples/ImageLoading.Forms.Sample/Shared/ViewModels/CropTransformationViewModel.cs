using System;
using DLToolkit.PageFactory;
using System.Collections.Generic;
using FFImageLoading.Work;

namespace FFImageLoading.Forms.Sample.ViewModels
{
	public class CropTransformationViewModel : BaseExampleViewModel
	{
		public CropTransformationViewModel()
		{
			
		}

		public override void PageFactoryMessageReceived(string message, object sender, object arg)
		{
			if (message == "Reload")
			{

			}
		}

		public string ImagePath {
			get { return GetField<string>(); }
			set { SetField(value); }
		}

		public List<ITransformation> Transformations {
			get { return GetField<List<ITransformation>>(); }
			set { SetField(value); }
		}
	}
}

