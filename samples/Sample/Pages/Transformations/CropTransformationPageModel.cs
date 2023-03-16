using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

		public ObservableCollection<ITransformation> Transformations { get; set; } = new ();

		[ObservableProperty]
        string imageUrl;


		public Action ReloadImageHandler { get; set; }

        public void ReloadImage()
        {
			ImageUrl = "https://loremflickr.com/600/600/nature?filename=crop_transformation.jpg";

			CurrentZoomFactor = 1d;
			CurrentXOffset = 0d;
			CurrentYOffset = 0d;

			Transformations.Clear();
            Transformations.Add(
                new CropTransformation(CurrentZoomFactor, CurrentXOffset, CurrentYOffset, 1f, 1f));

			ReloadImageHandler?.Invoke();
        }

        [ObservableProperty]
        double currentZoomFactor;

        [ObservableProperty]
        double currentXOffset;

        [ObservableProperty]
        double currentYOffset;

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
