using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ListView.Universal.Sample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<string> _urls = null;
        public ObservableCollection<string> URLs
        {
            get { return _urls ?? (_urls = new ObservableCollection<string>()); }
        }

        private DispatcherTimer _timer = null;
        private int _urlIdx = 0;
        private Random _random = new Random();

        public MainPage()
        {
            this.InitializeComponent();

            for (int i = 0; i < 5; i++)
                URLs.Add($"http://loremflickr.com/400/400?filename=simple.jpg?random={i}");

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 10); // 2 seconds
            _timer.Tick += _timer_Tick;
        }

        private void _timer_Tick(object sender, object e)
        {
            var idx = _random.Next(0, URLs.Count - 1);
            var rnd = _random.Next();
            URLs[0] = $"http://loremflickr.com/400/400?filename=simple.jpg?random={rnd}";

            /* if (_urlIdx < URLs.Count)
                ListView.ScrollIntoView(URLs[_urlIdx++]);
            else
                _timer.Stop();*/
        }

        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            _timer.Start();
        }

    }
}
