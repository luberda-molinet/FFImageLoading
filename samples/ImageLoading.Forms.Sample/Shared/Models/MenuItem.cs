using System;
using DLToolkit.PageFactory;
using System.Windows.Input;

namespace FFImageLoading.Forms.Sample.Models
{
	public class MenuItem : BaseModel
	{
		public MenuItem()
		{
		}

		string section;
		public string Section
		{
			get { return section; }
			set { SetField(ref section, value); }
		}

		string title;
		public string Title
		{
			get { return title; }
			set { SetField(ref title, value); }
		}

		string detail;
		public string Detail
		{
			get { return detail; }
			set { SetField(ref detail, value); }
		}

		ICommand command;
		public ICommand Command
		{
			get { return command; }
			set { SetField(ref command, value); }
		}

		object commandParameter;
		public object CommandParameter
		{
			get { return commandParameter; }
			set { SetField(ref commandParameter, value); }
		}
	}
}

