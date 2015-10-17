using System;
using System.Collections.Generic;
using DLToolkit.PageFactory;
using FFImageLoading.Work;
using FFImageLoading.Transformations;

namespace FFImageLoading.Forms.Sample.ViewModels
{
	public class TransformationExampleViewModel : PlaceholdersExampleViewModel
	{
		public TransformationExampleViewModel()
		{
			ErrorImagePath = "error.png";
			LoadingImagePath = "loading.png";
			Transformations = null;

			BlurredTransformationExampleCommand = new PageFactoryCommand(() => {
				Transformations = new List<ITransformation>() { 
					new FlipTransformation(FlipType.Vertical) 
				};
				ImagePath = GetRandomImageUrl();
			});

			CircleTransformationExampleCommand = new PageFactoryCommand(() => {
				Transformations = new List<ITransformation>() { 
					new CircleTransformation() 
				};
				ImagePath = GetRandomImageUrl();
			});

			ColorSpaceTransformationExampleCommand = new PageFactoryCommand(() => {
				Transformations = new List<ITransformation>() { 
					new ColorSpaceTransformation(FFColorMatrix.InvertColorMatrix) 
				};
				ImagePath = GetRandomImageUrl();
			});

			GrayscaleTransformationExampleCommand = new PageFactoryCommand(() => {
				Transformations = new List<ITransformation>() { 
					new GrayscaleTransformation() 
				};
				ImagePath = GetRandomImageUrl();
			});

			RoundedTransformationExampleCommand = new PageFactoryCommand(() => {
				Transformations = new List<ITransformation>() { 
					new RoundedTransformation(30) 
				};
				ImagePath = GetRandomImageUrl();
			});

			SepiaTransformationExampleCommand = new PageFactoryCommand(() => {
				Transformations = new List<ITransformation>() { 
					new SepiaTransformation() 
				};
				ImagePath = GetRandomImageUrl();
			});

			MultipleTransformationExampleCommand = new PageFactoryCommand(() => {
				Transformations = new List<ITransformation>() { 
					new ColorSpaceTransformation(FFColorMatrix.BlackAndWhiteColorMatrix), 
					new CircleTransformation() 
				};
				ImagePath = GetRandomImageUrl();
			});
		}

		public List<ITransformation> Transformations {
			get { return GetField<List<ITransformation>>(); }
			set { SetField(value); }
		}

		public IPageFactoryCommand BlurredTransformationExampleCommand { get; private set; }

		public IPageFactoryCommand CircleTransformationExampleCommand { get; private set; }

		public IPageFactoryCommand ColorSpaceTransformationExampleCommand { get; private set; }

		public IPageFactoryCommand GrayscaleTransformationExampleCommand { get; private set; }

		public IPageFactoryCommand RoundedTransformationExampleCommand { get; private set; }

		public IPageFactoryCommand SepiaTransformationExampleCommand { get; private set; }

		public IPageFactoryCommand MultipleTransformationExampleCommand { get; private set; }
	}
}

