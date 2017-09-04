using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Xamarin.Forms.Platform.WinRT;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace FFImageLoading.Forms.Sample.WinPhone
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : WindowsPhonePage
    {
        public MainPage()
        {
            this.InitializeComponent();
            LoadApplication(new FFImageLoading.Forms.Sample.App());
            //this.NavigationCacheMode = NavigationCacheMode.Required;
        }
    }
}
