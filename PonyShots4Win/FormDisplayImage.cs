using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PonyShots4Win
{
    public sealed partial class FormDisplayImage : Form
    {
        private enum State
        {
            IDLE,
            SELECTING,
            SELECTED,
            CROPPING,
            PAINTED_ONCE
        }
        private Point mouseDownPoint = Point.Empty;
        private Point mousePoint = Point.Empty;
        private SolidBrush semiTransBrush = new SolidBrush(Color.FromArgb(64, 0, 0, 0));
        private SolidBrush nullBrush = new SolidBrush(Color.FromArgb(0, 0, 0, 0));
        public Bitmap SelectedBitmap = null;
        private State state = State.IDLE;

        public bool HasSelection
        {
            get { return mouseDownPoint != mousePoint && SelectedBitmap != null; }
        }

        public FormDisplayImage(Image bmp)
        {
            InitializeComponent();
            BackgroundImage = bmp;
        }

        public void SetScreen(Screen screen)
        {
            StartPosition = FormStartPosition.Manual;

            var bounds = screen.Bounds;

            SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        private void FormDisplayImage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                //this.CropScreenShot();
                //this.Close();
                this.state = State.CROPPING;;
                this.Invalidate();
            }
        }

        private void CropScreenShot()
        {
            var upperLeft = new Point(Math.Min(mouseDownPoint.X, mousePoint.X), Math.Min(mouseDownPoint.Y, mousePoint.Y));
            var sz = new Size(Math.Abs(mouseDownPoint.X - mousePoint.X), Math.Abs(mouseDownPoint.Y - mousePoint.Y));
            var bitmap = new Bitmap(sz.Width, sz.Height);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(upperLeft, Point.Empty, sz);
            }
            SelectedBitmap = bitmap;
        }

        private void FormDisplayImage_MouseDown(object sender, MouseEventArgs e)
        {
            this.state = State.SELECTING;
            mousePoint = mouseDownPoint = e.Location;
        }

        private void FormDisplayImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.state == State.SELECTING)
            {
                this.mousePoint = MousePosition;
                this.Invalidate();
            }
        }

        private void FormDisplayImage_MouseUp(object sender, MouseEventArgs e)
        {
            this.state = State.SELECTED;
        }

        private void FormDisplayImage_Paint(object sender, PaintEventArgs e)
        {
            Region r = new Region(this.ClientRectangle);
            Rectangle window = new Rectangle(Math.Min(mouseDownPoint.X, mousePoint.X), Math.Min(mouseDownPoint.Y, mousePoint.Y), Math.Abs(mouseDownPoint.X - mousePoint.X), Math.Abs(mouseDownPoint.Y - mousePoint.Y));
            Region windowRegion = new Region(window);
            if (state == State.SELECTING)
            {
                e.Graphics.FillRegion(semiTransBrush, windowRegion);
            }
            else if (state == State.CROPPING)
            {
                e.Graphics.FillRegion(nullBrush, windowRegion);
                state = State.PAINTED_ONCE;
                this.Invalidate();
            }
            else if (state == State.PAINTED_ONCE)
            {
                CropScreenShot();
                this.Close();
            }
        }
    }
}
