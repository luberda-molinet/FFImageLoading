using System;
using System.Collections.Generic;

namespace Sample
{
    public partial class ListPageCell : ViewCell
    {
        public ListPageCell()
        {
            InitializeComponent();

            View.GestureRecognizers.Add(new TapGestureRecognizer()
            {
                Command = new Command((arg) =>
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

            image.Source = item.ImageUrl;
            label.Text = item.FileName;
        }
    }
}
