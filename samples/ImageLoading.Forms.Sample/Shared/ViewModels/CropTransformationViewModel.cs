using System;
using DLToolkit.PageFactory;
using System.Collections.Generic;
using FFImageLoading.Work;
using FFImageLoading.Transformations;

namespace FFImageLoading.Forms.Sample.ViewModels
{
	public class CropTransformationViewModel : BaseExampleViewModel
	{
		public CropTransformationViewModel()
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

		public override void PageFactoryMessageReceived(string message, object sender, object arg)
		{
			if (message == "Reload")
			{
				Transformations = null;
				CurrentZoomFactor = 1d;
				CurrentXOffset = 0d;
				CurrentYOffset = 0d;
				ImagePath = GetRandomImageUrl();
			}
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

		public List<ITransformation> Transformations {
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

		void ReloadImage()
		{
			var currentImage = ImagePath;

			Transformations = new List<ITransformation>() {
				new CropTransformation(CurrentZoomFactor, CurrentXOffset, CurrentYOffset, 1f, 1f)
			};	

			ImagePath = null;
			ImagePath = currentImage;
		}

		public IPageFactoryCommand AddCurrentZoomFactorCommad
		{
			get { return GetField<IPageFactoryCommand>(); }
			set { SetField(value); }
		}

		public IPageFactoryCommand AddCurrentXOffsetCommad
		{
			get { return GetField<IPageFactoryCommand>(); }
			set { SetField(value); }
		}

		public IPageFactoryCommand AddCurrentYOffsetCommad
		{
			get { return GetField<IPageFactoryCommand>(); }
			set { SetField(value); }
		}

		public IPageFactoryCommand SubCurrentZoomFactorCommad
		{
			get { return GetField<IPageFactoryCommand>(); }
			set { SetField(value); }
		}

		public IPageFactoryCommand SubCurrentXOffsetCommad
		{
			get { return GetField<IPageFactoryCommand>(); }
			set { SetField(value); }
		}

		public IPageFactoryCommand SubCurrentYOffsetCommad
		{
			get { return GetField<IPageFactoryCommand>(); }
			set { SetField(value); }
		}
	}
}

