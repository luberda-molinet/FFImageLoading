using Xamarin.Forms;
using FFImageLoading.Forms;

namespace Simple.TizenForms.Sample
{
    public class DownSampeTestPage : ContentPage
    {
        Label _label;
        public DownSampeTestPage()
        {
            Title = "DownSample Test";
            var img = new CachedImage
            {
                Source = "http://i.imgur.com/Ddqmjin.jpg",
                LoadingPlaceholder = "placeholder.jpg",
                DownsampleUseDipUnits = true,
                DownsampleWidth = 50,
            };

            img.Success += OnSuccess;
            img.Finish += OnFinish;

            _label = new Label();

            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children =
                {
                    img,
                    _label,
                }
            };
        }

        void OnFinish(object sender, CachedImageEvents.FinishEventArgs e)
        {
            _label.Text = "Finish";
        }

        void OnSuccess(object sender, CachedImageEvents.SuccessEventArgs e)
        {
            _label.Text = "Success";
        }
    }
}