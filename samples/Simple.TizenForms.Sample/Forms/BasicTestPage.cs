using Xamarin.Forms;
using FFImageLoading.Forms;

namespace Simple.TizenForms.Sample
{
    public class BasicTestPage : ContentPage
    {
        Label _label;
        public BasicTestPage()
        {
            Title = "Basic Test";
            var img = new CachedImage
            {
                Source = "http://i.imgur.com/Ddqmjin.jpg",
                LoadingPlaceholder = "placeholder.jpg",
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