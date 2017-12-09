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
			Application.Run(new ImageForm());
		}
	}
}
