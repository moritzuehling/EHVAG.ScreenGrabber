using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace EHVAG.ScreenGrabber
{
	public class SwayForm : ImageForm
	{
		Dictionary<string, Rectangle> SwayScreens = new Dictionary<string, Rectangle>();

		public SwayForm() : base(false, "swaymsg", "[title=\"^EHVAG_GLOBAL$\"] fullscreen")
		{
			GenerateSwayScreens();
			CaptureScreenTo();

			Initialize();

			this.Shown -= MakeFullscreen;
			this.Resize += MakeFullscreen;
		}

		public void CaptureScreenTo()
		{
			// Sway doesn't support global fullscreen (which is kinda dumb, tbh)
			// So we could either
			// (1) create a window for every output, and move stuff there
			// (2) simply only support capturing the active screen
			// 
			// (1) is harder to develop, and would need a lot of logic to handle
			//     the mouse crossing window borders etc, and would be ver specific
			// (2) doesn't need anything, but the detection which screen we're at is
			//     kinda hacky. We selected it anyway for now.

			var screenDump = GetStdOutOfAsByte("swaygrab", "--raw");

			var targetSize = screenDump.Length / 4;

			var screenRect = SwayScreens.Values.FirstOrDefault(a => a.Width * a.Height == targetSize);

			if (screenRect.IsEmpty)
			{
				Console.WriteLine("Could not find screen. Aborting.");
				Environment.Exit(1);
			}

			this.Bounds = screenRect;

			screenRect.X = 0;
			screenRect.Y = 0;

			CapturedImage = new Bitmap(Bounds.Width, Bounds.Height);

			unsafe
			{
				byte[] row = new byte[4 * screenRect.Width];
				var bits = CapturedImage.LockBits(screenRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
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

				CapturedImage.UnlockBits(bits);
			}
		}

		private void GenerateSwayScreens()
		{
			var rawJson = GetStdOutOf("swaymsg", "-t get_outputs -r");

			var swayMsg = JArray.Parse(rawJson);

			foreach (JObject screen in swayMsg)
			{
				var rect = screen["rect"];
				var srect = new Rectangle(rect["x"].Value<int>(), rect["y"].Value<int>(), rect["width"].Value<int>(), rect["height"].Value<int>());

				// Because sway doesn't support
				// [title="^EHVAG_GLOBAL$"] fullscreen toggle global
				// or anything similar, we can only capture the active
				// screen, and that's where we're also fullscreen'd.

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
