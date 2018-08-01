using Xamarin.Forms;
using FFImageLoading.Forms;
using FFImageLoading.Svg.Forms;

namespace Simple.TizenForms.Sample
{
    public class SvgBasicTestPage : ContentPage
    {
        Label _label;
        public SvgBasicTestPage()
        {
            Title = "SVG Basic Test";
            var img = new SvgCachedImage
            {
                Source = "sample.svg",
            };

            img.Success += OnSuccess;
            img.Finish += OnFinish;
            img.Error += OnError;

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

        void OnError(object sender, CachedImageEvents.ErrorEventArgs e)
        {
            _label.Text = $"Error : {e.Exception.Message}";
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