using System;
using DLToolkit.PageFactory;

namespace FFImageLoading.Forms.Sample.Models
{
	public class ListExampleItem : BaseModel
	{
		public ListExampleItem()
		{
		}

		string imageUrl;
		public string ImageUrl
		{
			get { return imageUrl; }
			set { SetField(ref imageUrl, value); }
		}

		string fileName;
		public string FileName
		{
			get { return fileName; }
			set { SetField(ref fileName, value); }
		}
	}
}

