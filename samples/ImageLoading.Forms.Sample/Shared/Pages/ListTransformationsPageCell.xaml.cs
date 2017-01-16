using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace FFImageLoading.Forms.Sample
{
	public partial class ListTransformationsPageCell : ViewCell
	{
		public ListTransformationsPageCell()
		{
			InitializeComponent();
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
