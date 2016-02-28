using System;
using System.Collections.Generic;
using DLToolkit.PageFactory;
using FFImageLoading.Work;
using FFImageLoading.Transformations;
using System.Windows.Input;

namespace FFImageLoading.Forms.Sample.PageModels
{
    public class TransformationPageModel : CommonPageModel
	{
		private readonly Random random = new Random();

		public TransformationPageModel()
		{
			ErrorImagePath = "error.png";
			LoadingImagePath = "loading.png";
			Transformations = null;

			LoadAnotherImageCommand = new PageFactoryCommand(() => {
				ImagePath = GetRandomImageUrl();
			});
		}

		public void ReloadTransformation(Type transformationType)
		{
			// RotateTransformation
			if (transformationType == typeof(RotateTransformation))
			{
				Transformations = new List<ITransformation>() { 
					new RotateTransformation(random.Next(60, 270), random.Next(2) == 0, random.Next(2) == 0) 
				};
			}

			// CircleTransformation
			if (transformationType == typeof(CircleTransformation))
			{
				Transformations = new List<ITransformation>() { 
					new CircleTransformation() 
				};
			}

			// RoundedTransformation
			if (transformationType == typeof(RoundedTransformation))
			{
				Transformations = new List<ITransformation>() { 
					new RoundedTransformation(30) 
				};
			}

			// CornersTransformation
			if (transformationType == typeof(CornersTransformation))
			{
				Transformations = new List<ITransformation>() { 
					new CornersTransformation(50, 0, 20, 30, 
						CornerTransformType.TopLeftCut | CornerTransformType.BottomLeftRounded | CornerTransformType.BottomRightCut)
				};
			}

			// GrayscaleTransformation
			if (transformationType == typeof(GrayscaleTransformation))
			{
				Transformations = new List<ITransformation>() { 
					new GrayscaleTransformation()
				};
			}

			// BlurredTransformation
			if (transformationType == typeof(BlurredTransformation))
			{
				Transformations = new List<ITransformation>() { 
					new BlurredTransformation(15)
				};
			}

			// SepiaTransformation
			if (transformationType == typeof(SepiaTransformation))
			{
				Transformations = new List<ITransformation>() { 
					new SepiaTransformation()
				};
			}

			// ColorSpaceTransformation
			if (transformationType == typeof(ColorSpaceTransformation))
			{
				Transformations = new List<ITransformation>() { 
					new ColorSpaceTransformation(FFColorMatrix.InvertColorMatrix)
				};
			}

			// FlipTransformation
			if (transformationType == typeof(FlipTransformation))
			{
				Transformations = new List<ITransformation>() { 
					new FlipTransformation(FlipType.Vertical)
				};
			}

			// MultipleTransformationsExample
			if (transformationType == null)
			{
				Transformations = new List<ITransformation>() {
					new CircleTransformation(),
					new ColorSpaceTransformation(FFColorMatrix.BlackAndWhiteColorMatrix),
				};

				TransformationType = "Multiple: CircleTransformation and ColorSpaceTransformation";
			}
			else
			{
				TransformationType = transformationType.Name;
			}

			ImagePath = GetRandomImageUrl();	
		}

        public void FreeResources()
        {
            ImagePath = null;
        }

		public List<ITransformation> Transformations 
        {
			get { return GetField<List<ITransformation>>(); }
			set { SetField(value); }
		}

		public string ImagePath 
        {
			get { return GetField<string>(); }
			set { SetField(value); }
		}

		public string TransformationType 
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

		public ICommand LoadAnotherImageCommand
        {
            get { return GetField<ICommand>(); }
            set { SetField(value); }
        }
	}
}

