using System;
using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.PageModels;

namespace FFImageLoading.Forms.Sample.Pages
{
    public partial class SimpleXamlPage : ContentPage, IBasePage<SimpleXamlPageModel>
    {
        public SimpleXamlPage()
        {
            InitializeComponent();
        }
    }
}

