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

			string fullScreenTool = null;
			if (args.Length > 0 && args[0] == "--i3")
			{
				fullScreenTool = "i3-msg";
			}

			if (args.Length > 0 && args[0] == "--sway")
				form = new SwayForm();
			else
				form = new ImageForm(fullScreenTool);

			Application.Run(form);
		}
	}
}
