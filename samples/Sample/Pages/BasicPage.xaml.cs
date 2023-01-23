using System;
using System.Collections.Generic;

namespace Sample
{
    public partial class BasicPage : ContentPage
    {
        public BasicPage()
        {
            InitializeComponent();
			BindingContext = new BasicPageModel();

		}
    }
}
