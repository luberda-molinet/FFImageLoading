using System;
using DLToolkit.PageFactory;
using System.Collections.Generic;
using FFImageLoading.Work;
using FFImageLoading.Transformations;
using System.Windows.Input;
using Xamarin.Forms;

namespace FFImageLoading.Forms.Sample.PageModels
{
    public class CropTransformationPageModel : CommonPageModel
	{
		double mX = 0f;
		double mY = 0f;
		double mRatioPan = -0.0015f;
		double mRatioZoom = 0.8f;

		public CropTransformationPageModel()
		{
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

		public void PanImage(PanUpdatedEventArgs e)
		{
			if (e.StatusType == GestureStatus.Completed) 
			{
				mX = CurrentXOffset;
				mY = CurrentYOffset;
			}
			else if (e.StatusType == GestureStatus.Running)
			{
				CurrentXOffset = (e.TotalX * mRatioPan) + mX;
				CurrentYOffset = (e.TotalY * mRatioPan) + mY;
				ReloadImage ();
			}
		}

		public void PinchImage(PinchGestureUpdatedEventArgs e)
		{
			if (e.Status == GestureStatus.Completed) 
			{
				mX = CurrentXOffset;
				mY = CurrentYOffset;
			}
			else if (e.Status == GestureStatus.Running) 
			{
				CurrentZoomFactor += (e.Scale - 1) * CurrentZoomFactor * mRatioZoom;
				CurrentZoomFactor = Math.Max (1, CurrentZoomFactor);

				CurrentXOffset = (e.ScaleOrigin.X * mRatioPan) + mX;
				CurrentYOffset = (e.ScaleOrigin.Y * mRatioPan) + mY;
				ReloadImage ();
			}
		}
	}
}

