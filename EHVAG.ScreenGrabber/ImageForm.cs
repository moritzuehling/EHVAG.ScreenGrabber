﻿using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

namespace EHVAG.ScreenGrabber
{
	public class ImageForm : Form
	{
		Bitmap CapturedImage;
		Bitmap OverlayImage;
		Graphics OverlayGraphics;

		SolidBrush BlackBrush = new SolidBrush(Color.FromArgb(40, Color.Black));
		SolidBrush WhiteBrush = new SolidBrush(Color.FromArgb(128, Color.White));
		Pen RedPen = new Pen(new SolidBrush(Color.Red), 1);

		Point Point1;
		Point Point2;
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
			OverlayGraphics = Graphics.FromImage(OverlayImage);
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
			Point1 = e.Location;

			IsSelecting = true;
			this.UploadButton.Hide();

			GenerateOverlayImage();
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
			using (var transparentBrush = new SolidBrush(Color.FromArgb(255, Color.Black)))
			{
				if (OldSelection.Width != 0 && OldSelection.Height != 0)
				{
					OverlayGraphics.DrawImage(CapturedImage, OldSelection, OldSelection, GraphicsUnit.Pixel);
				 	OverlayGraphics.FillRectangle(BlackBrush, OldSelection);
				 	OverlayGraphics.FillRectangle(WhiteBrush, OldSelection);
				}

				if (Selection.Width != 0 && Selection.Height != 0)
				{
					OverlayGraphics.DrawImage(CapturedImage, Selection, Selection, GraphicsUnit.Pixel);
					OverlayGraphics.DrawRectangle(RedPen, Selection.Left, Selection.Top, Selection.Width - 1, Selection.Height - 1);
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
