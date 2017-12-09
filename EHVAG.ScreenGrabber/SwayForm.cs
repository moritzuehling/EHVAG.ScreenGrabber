using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json.Linq;

namespace EHVAG.ScreenGrabber
{
	public class SwayForm : ImageForm
	{
		Dictionary<string, Rectangle> SwayScreens = new Dictionary<string, Rectangle>();

		public SwayForm() : base(false, "swaymsg")
		{
			GenerateSwayScreens();
			CaptureScreenshot();

			Initialize();
		}

		public void CaptureScreenshot()
		{
			this.Bounds = GetCompleteBounds();

			CapturedImage = new Bitmap(Bounds.Width, Bounds.Height);

			foreach (var screenName in SwayScreens.Keys)
				CaptureScreenTo(screenName, CapturedImage);

		}

		public void CaptureScreenTo(string screenName, Bitmap target)
		{
			var screenDump = GetStdOutOfAsByte("swaygrab", "--raw --output " + screenName);
			var screenRect = SwayScreens[screenName];

			unsafe
			{
				byte[] row = new byte[4 * screenRect.Width];
				var bits = target.LockBits(screenRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
				var scan0 = (byte*)bits.Scan0;

				var h = screenRect.Height;
				var maxY = h - 1;
				var w = screenRect.Width;

				fixed (byte* screenPtr = screenDump)
				{
					for (int x = 0; x < w; x++)
					{
						for (int y = 0; y < h; y++)
						{
							var screenOff = 4 * (x + w * (maxY - y));
							var bitmapOff = 3 * (x + w * y);

							scan0[bitmapOff + 2] = screenPtr[screenOff + 0];
							scan0[bitmapOff + 1] = screenPtr[screenOff + 1];
							scan0[bitmapOff + 0] = screenPtr[screenOff + 2];
						}
					}
				}

				target.UnlockBits(bits);
			}

			var screenSize = SwayScreens[screenName];
		}

		private void GenerateSwayScreens()
		{
			var rawJson = GetStdOutOf("swaymsg", "-t get_outputs -r");

			var swayMsg = JArray.Parse(rawJson);

			foreach (JObject screen in swayMsg)
			{
				var rect = screen["rect"];
				var srect = new Rectangle(rect["x"].Value<int>(), rect["y"].Value<int>(), rect["width"].Value<int>(), rect["height"].Value<int>());
				SwayScreens[screen["name"].Value<string>()] = srect;
			}
		}

		private Rectangle GetCompleteBounds()
		{
			int top = int.MaxValue;
			int left = int.MaxValue;
			int right = int.MinValue;
			int bottom = int.MinValue;

			foreach (var screen in SwayScreens)
			{
				Console.WriteLine("Screen {0}:", screen.Key);
				Console.WriteLine("  Rect: {0}", screen.Value.ToString());

				if (top > screen.Value.Left)
					top = screen.Value.Left;

				if (left > screen.Value.Left)
					left = screen.Value.Left;

				if (bottom < screen.Value.Bottom)
					bottom = screen.Value.Bottom;

				if (right < screen.Value.Right)
					right = screen.Value.Right;

			}

			var res = new Rectangle(top, left, right - left, bottom - top);

			Console.WriteLine("--------");
			Console.WriteLine("Final Bounds: {0}:", res);

			return res;
		}

		private string GetStdOutOf(string process, string args)
		{
			var psi = new ProcessStartInfo(process, args);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = true;

			var proc = Process.Start(psi);

			return proc.StandardOutput.ReadToEnd();
		}

		private byte[] GetStdOutOfAsByte(string process, string args)
		{
			var psi = new ProcessStartInfo(process, args);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = true;

			var proc = Process.Start(psi);

			using (var ms = new MemoryStream())
			{
				proc.StandardOutput.BaseStream.CopyTo(ms);
				return ms.ToArray();
			}
		}
	}
}
