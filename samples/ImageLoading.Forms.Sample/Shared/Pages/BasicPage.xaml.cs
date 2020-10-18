using System;
using System.Collections.Generic;

using Xamarin.Forms;
using System.Windows.Input;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
    public partial class BasicPage : ContentPage, IBasePage<BasicPageModel>
    {
        public BasicPage()
        {
            InitializeComponent();
        }
    }
}
