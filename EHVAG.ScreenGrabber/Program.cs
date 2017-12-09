using System;
using System.Windows.Forms;

namespace EHVAG.ScreenGrabber
{
	class MainClass
	{
		public static void Main(string[] args)
		{

			Application.SetCompatibleTextRenderingDefault(false);
			Application.EnableVisualStyles();

			ImageForm form;

			if (args.Length > 0 && args[0] == "--sway")
				form = new SwayForm();
			else
				form = new ImageForm();

			Application.Run(form);
		}
	}
}
