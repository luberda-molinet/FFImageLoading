using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamvvm;

namespace FFImageLoading.Forms.Sample
{
    public partial class ListPageCell : ViewCell
    {
        public ListPageCell()
        {
            InitializeComponent();

            View.GestureRecognizers.Add(new TapGestureRecognizer()
            {
                Command = new BaseCommand((arg) =>
                {
                    System.Diagnostics.Debug.WriteLine("TAPPED");
                })
            });
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            var item = BindingContext as ListPageModel.ListItem;
            if (item == null)
                return;

            Image.Source = item.ImageUrl;
            Label.Text = item.FileName;
        }
    }
}
