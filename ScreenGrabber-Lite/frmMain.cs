namespace ScreenGrabber_Lite
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Media;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    /// <summary>
    /// 
    /// </summary>
    public partial class frmMain : Form
    {
        #region - Constants -
        private const int SRCCOPY = 0xcc0020;
        #endregion

        #region - PInvoke -
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr GetDC(IntPtr hwnd);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hdcDest"></param>
        /// <param name="nXDest"></param>
        /// <param name="nYDest"></param>
        /// <param name="nWidth"></param>
        /// <param name="nHeight"></param>
        /// <param name="hdcSrc"></param>
        /// <param name="nXSrc"></param>
        /// <param name="nYSrc"></param>
        /// <param name="dwRop"></param>
        /// <returns></returns>
        [DllImport("gdi32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="hdc"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);
        #endregion

        #region - Private -
        private bool _isDrawing = false;
        private int _x1;
        private int _y1;
        private int _x2;
        private int _y2;
        private Bitmap _desktopBitmap;
        private Bitmap _selectedBitmap;
        private Graphics _desktopGraphics;
        #endregion

        #region - Constructor -
        /// <summary>
        /// 
        /// </summary>
        public frmMain()
        {
            InitializeComponent();
        }
        #endregion

        #region - Form Events -
        /// <summary>
        /// 
        /// </summary>
        /// <history>
        /// </history>
        private void frmMain_Load(object sender, EventArgs e)
        {
            Application.DoEvents();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <history>
        /// </history>
        private void frmMain_Shown(object sender, EventArgs e)
        {
            Opacity = 0;
            Hide();
            Application.DoEvents();
            notifyIcon1.Icon = Properties.Resources.ScreenGrabber;
            notifyIcon1.Text = String.Format("ScreenGrabberLite - {0} Screens", Screen.AllScreens.Length);
        }
        #endregion

        #region - Methods -

        /// <summary>
        /// 
        /// </summary>
        /// <history>
        /// 09.01.2010 - Justin R. LaBenne - Added CompositingQuality,InterpolationMode,SmoothingMode
        /// </history>
        private void startingToGrab(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _isDrawing = true;
            _x1 = e.X;
            _x2 = e.X;
            _y1 = e.Y;
            _y2 = e.Y;
            _desktopGraphics = picDesktop.CreateGraphics();
            _desktopGraphics.CompositingQuality = CompositingQuality.HighQuality;
            _desktopGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            _desktopGraphics.SmoothingMode = SmoothingMode.HighQuality;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <history>
        /// 04.12.2009 - Justin R. LaBenne - DashStyle
        /// </history>
        private void currentlyGrabbing(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!_isDrawing) return;

            _x2 = e.X;
            _y2 = e.Y;

            _desktopGraphics.DrawImage(_desktopBitmap, 0, 0);
            Rectangle rect = new Rectangle(Math.Min(_x1, _x2), Math.Min(_y1, _y2), Math.Abs((_x1 - _x2)), Math.Abs((_y1 - _y2)));
            using (Pen p = new Pen(Color.Green, 2))
            {
                p.DashStyle = DashStyle.Dash;
                _desktopGraphics.DrawRectangle(p, rect);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <history>
        /// 04.28.2008 - Justin R. LaBenne - Detect if width or height is 0 (Error if not) discovered by Lee Beckman and Adam Miller
        /// 02.06.2009 - Justin R. LaBenne - Form Opacity to zero added
        /// 09.27.2010 - Justin R. LaBenne - Changed filename increment to start with "1" instead of "0"
        /// </history>
        private void doneGrabbing(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!_isDrawing) return;

            _isDrawing = false;

            Cursor.Current = Cursors.WaitCursor;
            Opacity = 0;
            Hide();

            try
            {
                Rectangle rect = new Rectangle(Math.Min(_x1, _x2), Math.Min(_y1, _y2), Math.Abs(_x1 - _x2), Math.Abs(_y1 - _y2));

                if (rect.Width == 0 | rect.Height == 0)
                {
                    MessageBox.Show("Invalid selection", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _selectedBitmap = (Bitmap)_desktopBitmap.Clone(rect, _desktopBitmap.PixelFormat);

                SoundPlayer spAsync = new SoundPlayer(Properties.Resources.snapshot);
                spAsync.PlaySync();

                //Clipboard.SetDataObject(_selectedBitmap);

                string path = AppDomain.CurrentDomain.BaseDirectory + "ScreenGrabs";
                string dateString = DateTime.Today.ToShortDateString().Replace("/", "-");
                path = Path.Combine(path, dateString);
                if (!path.EndsWith(Path.DirectorySeparatorChar.ToString())) path += Path.DirectorySeparatorChar.ToString();

                // TODO: Need to add a better trap for access denied potential issue
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);


                string file = string.Empty;
                string ext = ".jpg";

                for (int i = 1; i <= 9999999; i++)
                {
                    file = path + i.ToString() + ext;
                    if (!File.Exists(file)) break;
                }

                _selectedBitmap.Save(file, ImageFormat.Jpeg);
                _selectedBitmap.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;

                if ((_desktopBitmap != null))
                    _desktopBitmap.Dispose();

                if ((_desktopGraphics != null))
                    _desktopGraphics.Dispose();

                _desktopBitmap = null;
                _desktopGraphics = null;

                if ((picDesktop.Image != null))
                    picDesktop.Image.Dispose();

                picDesktop.Image = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static Bitmap CaptureDesktopImage(Rectangle bounds)
        {
            Bitmap currentImage = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics currentImageGraphic = Graphics.FromImage(currentImage))
            {
                IntPtr currentImageHdc = IntPtr.Zero;
                IntPtr desktopHdc = IntPtr.Zero;
                try
                {
                    currentImageHdc = currentImageGraphic.GetHdc();
                    desktopHdc = GetDC(IntPtr.Zero);
                    BitBlt(currentImageHdc, 0, 0, currentImage.Width, currentImage.Height, desktopHdc, bounds.X, 0, SRCCOPY);
                    return currentImage;
                }
                finally
                {
                    currentImageGraphic.ReleaseHdc();
                    ReleaseDC(IntPtr.Zero, desktopHdc);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!(_desktopBitmap == null))
                _desktopBitmap.Dispose();

            if (!(_desktopGraphics == null))
                _desktopGraphics.Dispose();

            _desktopBitmap = null;
            _desktopGraphics = null;

            Rectangle bounds = new Rectangle();
            foreach (Screen scr in Screen.AllScreens)
                bounds = Rectangle.Union(bounds, scr.Bounds);

            _desktopBitmap = CaptureDesktopImage(bounds);

            Top = bounds.Top;
            Left = bounds.Left;
            Width = bounds.Width;
            Height = bounds.Height;
            Show();
            Opacity = 1;
            picDesktop.Image = (Bitmap)_desktopBitmap.Clone();
        }
        #endregion

        #region - Context Menu -
        /// <summary>
        /// Shutdown the application from tray icon context menu item "Close"
        /// </summary>
        /// <history>
        /// </history>
        private void ctxMenuClose_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false; // Remove tray icon
            Application.DoEvents(); // Ensure it tries real hard to disappear
            Close();
        }
        #endregion
    }
}
