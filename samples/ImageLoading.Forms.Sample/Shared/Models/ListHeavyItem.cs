using System;
using DLToolkit.PageFactory;

namespace FFImageLoading.Forms.Sample.Models
{
	public class ListHeavyItem : BaseModel
	{
		public ListHeavyItem()
		{
		}

		string image1Url;
		public string Image1Url
		{
			get { return image1Url; }
			set { SetField(ref image1Url, value); }
		}

		string image2Url;
		public string Image2Url
		{
			get { return image2Url; }
			set { SetField(ref image2Url, value); }
		}

		string image3Url;
		public string Image3Url
		{
			get { return image3Url; }
			set { SetField(ref image3Url, value); }
		}

		string image4Url;
		public string Image4Url
		{
			get { return image4Url; }
			set { SetField(ref image4Url, value); }
		}
	}
}

