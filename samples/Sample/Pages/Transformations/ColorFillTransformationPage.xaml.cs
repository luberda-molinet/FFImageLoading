using System;
using System.Collections.Generic;

namespace Sample.Pages.Transformations
{
    public partial class ColorFillTransformationPage : ContentPage
    {
        public ColorFillTransformationPage()
        {
            InitializeComponent();
			BindingContext = new ColorFillTransformationPageModel();
        }
    }
}
