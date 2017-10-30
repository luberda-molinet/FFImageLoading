using FFImageLoading.Work;
using FFImageLoading.Transformations;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Simple.Universal.Sample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        List<ITransformation> transformationsBin1 = new List<ITransformation>()
        {
            new GrayscaleTransformation(),
            new CircleTransformation(),
        };

        List<ITransformation> transformationsBin2 = new List<ITransformation>()
        {
            new RotateTransformation(45),
        };

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Image.Transformations = transformationsBin1;
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Image.Transformations = transformationsBin2;
        }
    }
}
