using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FFImageLoading.Transformations;
using FFImageLoading.Work;

namespace Sample
{
    
    public partial class CropTransformationPageModel : ObservableObject
    {
        double mX = 0f;
        double mY = 0f;
        double mRatioPan = -0.0015f;
        double mRatioZoom = 0.8f;

		[ObservableProperty]
		List<ITransformation> transformations = new List<ITransformation>();

		[ObservableProperty]
        string imageUrl = "http://loremflickr.com/600/600/nature?filename=crop_transformation.jpg";


		public Action ReloadImageHandler { get; set; }


        public void Reload()
        {
            CurrentZoomFactor = 1d;
            CurrentXOffset = 0d;
            CurrentYOffset = 0d;
        }

        void ReloadImage()
        {
            Transformations = new List<ITransformation>() {
                new CropTransformation(CurrentZoomFactor, CurrentXOffset, CurrentYOffset, 1f, 1f)
            };

			ReloadImageHandler?.Invoke();
        }

        public double CurrentZoomFactor { get; set; }

        public double CurrentXOffset { get; set; }

        public double CurrentYOffset { get; set; }

        public void OnPanUpdated(PanUpdatedEventArgs e)
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
                ReloadImage();
            }
        }

        public void OnPinchUpdated(PinchGestureUpdatedEventArgs e)
        {
            if (e.Status == GestureStatus.Completed)
            {
                mX = CurrentXOffset;
                mY = CurrentYOffset;
            }
            else if (e.Status == GestureStatus.Running)
            {
                CurrentZoomFactor += (e.Scale - 1) * CurrentZoomFactor * mRatioZoom;
                CurrentZoomFactor = Math.Max(1, CurrentZoomFactor);

                CurrentXOffset = (e.ScaleOrigin.X * mRatioPan) + mX;
                CurrentYOffset = (e.ScaleOrigin.Y * mRatioPan) + mY;
                ReloadImage();
            }
        }
    }
}
