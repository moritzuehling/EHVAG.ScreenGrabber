using System;
using System.Diagnostics;
using System.IO;

namespace EHVAG.ScreenGrabber
{
	public static class MultiPlatformClipboard
	{
		public static void SetText(string text)
		{
			var cliptool = Environment.GetEnvironmentVariable("CLIPTOOL");
			if (cliptool == "xclip")
			{
				XClipText(text);
			}
			else if (cliptool == "xsel")
			{
				XSelText(text);
			}
			else
			{
				System.Windows.Forms.Clipboard.SetText(text);
			}
		}

		public static void SetBitmap(System.Drawing.Bitmap bmp)
		{
			var cliptool = Environment.GetEnvironmentVariable("CLIPTOOL");
			if (cliptool == "xclip")
			{
				XClipImage(bmp);
			}
			else if (cliptool == "xsel")
			{
				XSelImage(bmp);
			}
			else
			{
				System.Windows.Forms.Clipboard.SetImage(bmp);
			}
		}

		private static void XClipText(string text)
		{
			var psi = new ProcessStartInfo("xclip", "-selection clipboard");
			psi.UseShellExecute = false;
			psi.RedirectStandardInput = true;

			var proc = Process.Start(psi);
			proc.StandardInput.Write(text);
			proc.StandardInput.Close();
			proc.WaitForExit();
		}

		private static void XClipImage(System.Drawing.Bitmap bmp)
		{
			var psi = new ProcessStartInfo("xclip", "-selection clipboard -t image/png");
			psi.UseShellExecute = false;
			psi.RedirectStandardInput = true;

			using (var ms = new MemoryStream())
			{
				bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
				var proc = Process.Start(psi);

				var data = ms.ToArray();

				proc.StandardInput.BaseStream.Write(data, 0, data.Length);
				proc.StandardInput.BaseStream.Close();
				proc.WaitForExit();
			}


		}
	
		private static void XSelText(string text)
		{
			var psi = new ProcessStartInfo("xsel", "--clipboard");
			psi.UseShellExecute = false;
			psi.RedirectStandardInput = true;

			var proc = Process.Start(psi);
			proc.StandardInput.Write(text);
			proc.StandardInput.Close();
			proc.WaitForExit();
		}

		private static void XSelImage(System.Drawing.Bitmap bmp)
		{
			Console.WriteLine("xsel isn't working with images, pal.");
		}
	}
}
