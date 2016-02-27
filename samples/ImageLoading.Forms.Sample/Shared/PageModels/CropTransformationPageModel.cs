using System;
using DLToolkit.PageFactory;
using System.Collections.Generic;
using FFImageLoading.Work;
using FFImageLoading.Transformations;
using System.Windows.Input;

namespace FFImageLoading.Forms.Sample.PageModels
{
    public class CropTransformationPageModel : CommonPageModel
	{
		public CropTransformationPageModel()
		{
			AddCurrentZoomFactorCommad = new PageFactoryCommand(() => {
				
				if (CurrentZoomFactor + 0.1d >= 1d)
					CurrentZoomFactor += 0.1d;
				ReloadImage();

			});

			SubCurrentZoomFactorCommad = new PageFactoryCommand(() => {

				if (CurrentZoomFactor - 0.1d >= 1d)
					CurrentZoomFactor -= 0.1d;
				ReloadImage();

			});

			AddCurrentXOffsetCommad = new PageFactoryCommand(() => {

				CurrentXOffset += 0.05d;
				ReloadImage();

			});

			SubCurrentXOffsetCommad = new PageFactoryCommand(() => {

				CurrentXOffset -= 0.05d;
				ReloadImage();

			});

			AddCurrentYOffsetCommad = new PageFactoryCommand(() => {

				CurrentYOffset += 0.05d;
				ReloadImage();

			});

			SubCurrentYOffsetCommad = new PageFactoryCommand(() => {

				CurrentYOffset -= 0.05d;
				ReloadImage();

			});
		}

        void ReloadImage()
        {
            var currentImage = ImagePath;

            Transformations = new List<ITransformation>() {
                new CropTransformation(CurrentZoomFactor, CurrentXOffset, CurrentYOffset, 1f, 1f)
            };  

            ImagePath = null;
            ImagePath = currentImage;
        }

        public void Reload()
        {
            Transformations = null;
            CurrentZoomFactor = 1d;
            CurrentXOffset = 0d;
            CurrentYOffset = 0d;
            ImagePath = GetRandomImageUrl();
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

		public List<ITransformation> Transformations 
        {
			get { return GetField<List<ITransformation>>(); }
			set { SetField(value); }
		}

		public double CurrentZoomFactor
		{
			get { return GetField<double>(); }
			set { SetField(value); }
		}

		public double CurrentXOffset
		{
			get { return GetField<double>(); }
			set { SetField(value); }
		}

		public double CurrentYOffset
		{
			get { return GetField<double>(); }
			set { SetField(value); }
		}

		public ICommand AddCurrentZoomFactorCommad
		{
            get { return GetField<ICommand>(); }
			set { SetField(value); }
		}

        public ICommand AddCurrentXOffsetCommad
		{
            get { return GetField<ICommand>(); }
			set { SetField(value); }
		}

        public ICommand AddCurrentYOffsetCommad
		{
            get { return GetField<ICommand>(); }
			set { SetField(value); }
		}

        public ICommand SubCurrentZoomFactorCommad
		{
            get { return GetField<ICommand>(); }
			set { SetField(value); }
		}

        public ICommand SubCurrentXOffsetCommad
		{
            get { return GetField<ICommand>(); }
			set { SetField(value); }
		}

        public ICommand SubCurrentYOffsetCommad
		{
            get { return GetField<ICommand>(); }
			set { SetField(value); }
		}
	}
}

