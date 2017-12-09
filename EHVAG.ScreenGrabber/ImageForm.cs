using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

namespace EHVAG.ScreenGrabber
{
	public class ImageForm : Form
	{
		Bitmap CapturedImage;
		Bitmap OverlayImage;

		Rectangle Selection = new Rectangle(0, 0, 0, 0);
		Rectangle OldSelection = new Rectangle(0, 0, 0, 0);
		bool IsSelecting = false;

		PictureBox ImageContainer = new PictureBox();

		Button UploadButton = new Button();

		public ImageForm() 
		{
			this.KeyDown += Handle_KeyDown;

			Text = "EHVAG_GLOBAL";

			this.WindowState = FormWindowState.Normal;
			this.FormBorderStyle = FormBorderStyle.None;
			this.WindowState = FormWindowState.Maximized;
			this.Bounds = GetCompleteBounds();

			CapturedImage = new Bitmap(Bounds.Width, Bounds.Height);
			OverlayImage = new Bitmap(Bounds.Width, Bounds.Height);
			OldSelection = new Rectangle(0, 0, Bounds.Width, Bounds.Height);

			using (var g = Graphics.FromImage(CapturedImage))
			{
				g.CopyFromScreen(this.Bounds.Location, Point.Empty, this.Bounds.Size);
			}

			ImageContainer.Dock = DockStyle.Fill;
			this.Controls.Add(ImageContainer);
			this.ImageContainer.MouseDown += Handle_MouseDown;
			this.ImageContainer.MouseMove += Handle_MouseMove;
			this.ImageContainer.MouseUp += Handle_MouseUp;

			this.UploadButton.Text = "Upload!";
			this.UploadButton.Size = new Size(100, 30);
			this.UploadButton.Hide();
			this.UploadButton.KeyDown += Handle_KeyDown;

			this.ImageContainer.Controls.Add(this.UploadButton);

			GenerateOverlayImage();
		}

		void Handle_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				Environment.Exit(0);
		}

		void Handle_MouseDown(object sender, MouseEventArgs e)
		{
			Selection.Location = e.Location;
			Selection.Size = Size.Empty;
			IsSelecting = true;
			this.UploadButton.Hide();

			GenerateOverlayImage();
		}

		void Handle_MouseMove(object sender, MouseEventArgs e)
		{
			if (!IsSelecting)
				return;

			var size = new Size(e.Location.X - Selection.Location.X, e.Location.Y - Selection.Location.Y);
			Selection.Size = size;

			if (Selection.Width < 0)
			{
				Selection.X += Selection.Width;
				Selection.Width = -Selection.Width;
			}

			if (Selection.Height < 0)
			{
				Selection.Y += Selection.Height;
				Selection.Height = -Selection.Height;
			}
			GenerateOverlayImage();
		}

		void Handle_MouseUp(object sender, MouseEventArgs e)
		{
			Handle_MouseMove(sender, e);
			IsSelecting = false;

			GenerateOverlayImage();

			if (Selection.Width > 0 && Selection.Height > 0)
			{
				var x = (Selection.Left + Selection.Width / 2) - this.UploadButton.Width / 2;
				var y = (Selection.Top + Selection.Height / 2) - this.UploadButton.Height / 2;
				this.UploadButton.Location = new Point(x, y);
				this.UploadButton.Show();
			}
		}

		public void GenerateOverlayImage()
		{
			using (var g = Graphics.FromImage(OverlayImage))
			using (var transparentBrush = new SolidBrush(Color.FromArgb(255, Color.Black)))
			using (var blackBrush = new SolidBrush(Color.FromArgb(40, Color.Black)))
			using (var whiteBrush = new SolidBrush(Color.FromArgb(128, Color.White)))
			using (var redBrush = new SolidBrush(Color.Red))
			using (var redPen = new Pen(redBrush, 1))
			{
				if (OldSelection.Width != 0 && OldSelection.Height != 0)
				{
					g.DrawImage(CapturedImage, OldSelection, OldSelection, GraphicsUnit.Pixel);
				 	g.FillRectangle(blackBrush, OldSelection);
				 	g.FillRectangle(whiteBrush, OldSelection);
				}

				if (Selection.Width != 0 && Selection.Height != 0)
				{
					g.DrawImage(CapturedImage, Selection, Selection, GraphicsUnit.Pixel);
					g.DrawRectangle(redPen, Selection.Left, Selection.Top, Selection.Width - 1, Selection.Height - 1);
				}
			}

				OldSelection = Selection;
			this.ImageContainer.Image = OverlayImage;
		}

		public Rectangle GetCompleteBounds()
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
	}
}
