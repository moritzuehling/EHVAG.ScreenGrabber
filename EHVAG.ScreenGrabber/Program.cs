using System;
using System.IO;
using System.Windows.Forms;

namespace EHVAG.ScreenGrabber
{
	class MainClass
	{
		public static ConfigEntry[] Config;

		public static void Main(string[] args)
		{

			Application.SetCompatibleTextRenderingDefault(false);
			Application.EnableVisualStyles();

			if (args.Length == 0 || !File.Exists(args[0]))
			{
				Console.WriteLine("config-file must be first argument.");
				return;
			}

			string configFileText = File.ReadAllText(args[0]);

			Config = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigEntry[]>(configFileText);

			var wm = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");

			ImageForm form;

			string fullScreenTool = null, fullScreenToolArgs = null;
			if (wm == "i3")
			{
				fullScreenTool = "i3-msg";
				fullScreenToolArgs = "[title=\"^EHVAG_GLOBAL$\"] fullscreen global";
			}

			if (wm == "sway")
				form = new SwayForm();
			else
				form = new ImageForm(fullScreenTool, fullScreenToolArgs);

			Application.Run(form);
		}
	}
}
