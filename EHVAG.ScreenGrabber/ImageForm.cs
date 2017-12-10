using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace EHVAG.ScreenGrabber
{
	public class ImageForm : Form
	{
		protected Bitmap CapturedImage;
		Bitmap OverlayImage;
		Graphics OverlayGraphics;

		SolidBrush WhiteBrush = new SolidBrush(Color.FromArgb(40, Color.White));
		SolidBrush BlackBrush = new SolidBrush(Color.FromArgb(40, Color.Black));
		Pen RedPen = new Pen(new SolidBrush(Color.Red), 1);

		Point Point1;
		Point Point2;
		Rectangle Selection = new Rectangle(0, 0, 0, 0);
		Rectangle OldSelection = new Rectangle(0, 0, 0, 0);
		bool IsSelecting = false;

		PictureBox ImageContainer = new PictureBox();

		Button UploadButton = new Button();

		string FullscreenTool;
		string FullscreenToolArgs;

		bool WasResized = false;

		Timer InitTimer = new Timer();

		public ImageForm(string fullscreenTool, string fullscreenToolArgs) : this(true, fullscreenTool, fullscreenToolArgs)
		{
			Initialize();
		}

		protected ImageForm(bool captureScreenshot, string fullscreenTool, string fullscreenToolArgs)
		{
			if (captureScreenshot)
				CaptureScreenshot();

			FullscreenTool = fullscreenTool;
			FullscreenToolArgs = fullscreenToolArgs;
		}

		protected void Initialize()
		{
			this.Text = "EHVAG_GLOBAL";
			this.KeyDown += Handle_KeyDown;

			this.WindowState = FormWindowState.Normal;
			this.FormBorderStyle = FormBorderStyle.None;
			this.WindowState = FormWindowState.Maximized;

			this.BackColor = Color.White;

			InitTimer.Tick += InitTimer_Tick;
			InitTimer.Interval = 16;
			InitTimer.Enabled = true;

			this.Shown += MakeFullscreen;

			// this.ImageContainer.Image = CapturedImage;
			// GenerateOverlayImage();
		}

		void FullyInitialize()
		{
			OverlayImage = new Bitmap(CapturedImage.Width, CapturedImage.Height);
			OverlayGraphics = Graphics.FromImage(OverlayImage);

			ImageContainer.Dock = DockStyle.Fill;
			this.Controls.Add(ImageContainer);
			this.ImageContainer.MouseDown += Handle_MouseDown;
			this.ImageContainer.MouseMove += Handle_MouseMove;
			this.ImageContainer.MouseUp += Handle_MouseUp;

			this.UploadButton.Text = MainClass.Config[0].Name;
			this.UploadButton.Tag = MainClass.Config[0];
			this.UploadButton.Size = new Size(100, 30);
			this.UploadButton.Hide();
			this.UploadButton.KeyDown += Handle_KeyDown;
			this.UploadButton.BackColor = Color.LightGray;
			this.UploadButton.Click += AnyUploadButton_Click;

			this.ImageContainer.Controls.Add(this.UploadButton);
			this.ImageContainer.BackColor = Color.FromArgb(100, 230, 230, 230);

			OldSelection = new Rectangle(Point.Empty, CapturedImage.Size);
		}

		void InitTimer_Tick(object sender, EventArgs e)
		{
			FullyInitialize();
			this.BackgroundImage = CapturedImage;
			this.InitTimer.Enabled = false;
		}

		public void MakeFullscreen(object sender, EventArgs e)
		{
			if (FullscreenTool != null && !WasResized)
			{
				WasResized = true;
				Process.Start(FullscreenTool, FullscreenToolArgs);
			}
		}

		void CaptureScreenshot()
		{
			this.Bounds = GetCompleteBounds();

			CapturedImage = new Bitmap(Bounds.Width, Bounds.Height);

			using (var g = Graphics.FromImage(CapturedImage))
			{
				g.CopyFromScreen(Bounds.Location, Point.Empty, this.Bounds.Size);
			}
		}

		void Handle_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				Environment.Exit(0);
		}

		void Handle_MouseDown(object sender, MouseEventArgs e)
		{
			Point1 = e.Location;

			IsSelecting = true;
			this.UploadButton.Hide();

			UpdateOverlay(true);
		}

		void Handle_MouseMove(object sender, MouseEventArgs e)
		{
			if (!IsSelecting)
				return;

			Point2 = e.Location;

			var left = Math.Min(Point1.X, Point2.X);
			var right = Math.Max(Point1.X, Point2.X);

			var top = Math.Min(Point1.Y, Point2.Y);
			var bottom = Math.Max(Point1.Y, Point2.Y);

			Selection = new Rectangle(left, top, right - left, bottom - top);

			UpdateOverlay(false);
		}

		void Handle_MouseUp(object sender, MouseEventArgs e)
		{
			Handle_MouseMove(sender, e);
			IsSelecting = false;

			if (Selection.Width > 0 && Selection.Height > 0)
			{
				var x = (Selection.Left + Selection.Width / 2) - this.UploadButton.Width / 2;
				var y = (Selection.Top + Selection.Height / 2) - this.UploadButton.Height / 2;
				this.UploadButton.Location = new Point(x, y);
				this.UploadButton.Show();
			}
		}

		public void UpdateOverlay(bool clear)
		{
			using (var transparentBrush = new SolidBrush(Color.FromArgb(255, Color.Black)))
			{
				if (Selection.Width != 0 && Selection.Height != 0)
				{
					if (clear || true)
					{
						OverlayGraphics.Clip = new Region(OldSelection);
						OverlayGraphics.Clear(Color.Transparent);
						OverlayGraphics.Clip = new Region(Selection);
						OverlayGraphics.DrawImage(CapturedImage, Selection, Selection, GraphicsUnit.Pixel);
					}
					else
					{
						// Smart redraw
					}

					OverlayGraphics.Clip = new Region(Selection);
					OverlayGraphics.DrawRectangle(RedPen, Selection.Left, Selection.Top, Selection.Width - 1, Selection.Height - 1);
				}
			}
			OldSelection = Selection;
			this.ImageContainer.Image = OverlayImage;
		}

		private Rectangle GetCompleteBounds()
		{
			int top = int.MaxValue;
			int left = int.MaxValue;
			int right = int.MinValue;
			int bottom = int.MinValue;

			int i = 0;
			foreach (var screen in Screen.AllScreens)
			{
				Console.WriteLine("Screen #{0}:", i);
				Console.WriteLine("  WorkingArea: {0}", screen.WorkingArea.ToString());
				Console.WriteLine("       Bounds: {0}", screen.Bounds.ToString());

				if (top > screen.Bounds.Left)
					top = screen.Bounds.Left;

				if (left > screen.Bounds.Left)
					left = screen.Bounds.Left;

				if (bottom < screen.Bounds.Bottom)
					bottom = screen.Bounds.Bottom;

				if (right < screen.Bounds.Right)
					right = screen.Bounds.Right;

				i++;
			}

			var res = new Rectangle(top, left, right - left, bottom - top);

			Console.WriteLine("--------");
			Console.WriteLine("Final Bounds: {0}:", res);

			return res;
		}

		async void AnyUploadButton_Click(object sender, EventArgs e)
		{
			var ctrl = ((Control)sender);
			ConfigEntry targetEntry = (ConfigEntry)(ctrl.Tag);
			ctrl.Enabled = false;

			var img = new Bitmap(Selection.Width, Selection.Height);
			var g = Graphics.FromImage(img);
			g.DrawImage(CapturedImage, new Rectangle(Point.Empty, img.Size), Selection, GraphicsUnit.Pixel);


			var res = await ImageUpload.Upload(img, targetEntry);

			g.Dispose();
			img.Dispose();

			ctrl.Enabled = true;
			Clipboard.SetText(res.Link);
			var deleteLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ehvag_delete.log");
			File.AppendAllText(deleteLog, res.Link + "|" + res.DeleteUrl + Environment.NewLine);
		}
	}
}
