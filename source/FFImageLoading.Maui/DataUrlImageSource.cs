using System;

namespace FFImageLoading.Maui
{
    [Preserve(AllMembers = true)]
    public class DataUrlImageSource : ImageSource
    {
        public DataUrlImageSource(string dataUrl)
        {
            DataUrl = dataUrl;
        }

        public static readonly BindableProperty DataUrlProperty = BindableProperty.Create(nameof(DataUrl), typeof(string), typeof(DataUrlImageSource), default(string));

        public string DataUrl
        {
            get => (string)GetValue(DataUrlProperty);
            set => SetValue(DataUrlProperty, value);
        }

        public override string ToString()
        {
            return $"DataUrlImageSource: {DataUrl}";
        }
    }
}
