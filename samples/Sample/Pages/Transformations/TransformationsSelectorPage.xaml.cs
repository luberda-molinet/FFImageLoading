using System;
using System.Collections.Generic;

namespace Sample
{
    public partial class TransformationsSelectorPage : ContentPage
    {
        public TransformationsSelectorPage()
        {
            InitializeComponent();
			BindingContext = new TransformationsSelectorPageModel();

		}
    }
}
