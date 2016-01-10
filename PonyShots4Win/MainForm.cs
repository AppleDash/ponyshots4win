using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace PonyShots4Win
{
    public partial class MainForm : Form
    {
        private static readonly string BASE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "PonyShots");
        private static readonly string CONFIG_FILE = Path.Combine(BASE_PATH, "ponyshots4win.json");
        private bool _exiting = false;
        private readonly PonyShots _ponyShots;
        private readonly Config _config;
        private readonly HotKeys _hotKeys;

        public MainForm()
        {
            InitializeComponent();
            if (!Directory.Exists(BASE_PATH))
            {
                Directory.CreateDirectory(BASE_PATH);
            }

            if (File.Exists(CONFIG_FILE))
            {
                try
                {
                    _config = Config.Parse(CONFIG_FILE);
                }
                catch (Exception e)
                {
                    HandleError("Failed to parse _config, loading defaults:", e);
                    _config = Config.Default();
                    _config.Save(CONFIG_FILE);
                }
            }
            else
            {
                _config = Config.Default();
                _config.Save(CONFIG_FILE);
                MessageBox.Show($"You had no config; I created a default one for you.\r\nYou probably want to edit '{CONFIG_FILE}'.", "PonyShots4Win");
            }

            _ponyShots = new PonyShots
            {
                UploadUrl = _config.UploadUrl,
                ImageBaseUrl = _config.BaseUrl,
                Username = _config.Username,
                ApiKey = _config.ApiKey
            };

            _hotKeys = new HotKeys(Handle);
            _hotKeys.OnHotKey += this.OnHotKey;

            foreach (var k in new Keys[] {Keys.D2, Keys.D3, Keys.D4, Keys.D5})
            {
                _hotKeys.RegisterHotKey(k, KeyModifier.Control | KeyModifier.Shift);
            }

            /*
            WinApi.RegisterHotKey(this.Handle, 0, ((int)KeyModifier.Control | (int)KeyModifier.Shift), Keys.D2.GetHashCode());
            WinApi.RegisterHotKey(this.Handle, 1, ((int)KeyModifier.Control | (int)KeyModifier.Shift), Keys.D3.GetHashCode());
            WinApi.RegisterHotKey(this.Handle, 2, ((int)KeyModifier.Control | (int)KeyModifier.Shift), Keys.D4.GetHashCode());
            WinApi.RegisterHotKey(this.Handle, 3, ((int)KeyModifier.Control | (int)KeyModifier.Shift), Keys.D5.GetHashCode());
            */
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _exiting = true;
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this._exiting)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            if (!this.Visible)
            {
                this.Show();
            }
        }

        private void DisplayUploadedNotification(string url)
        {
            //trayIcon.Visible = true;
            //trayIcon.BalloonTipTitle = "Screenshot Uploaded!";
            //trayIcon.BalloonTipText = string.Format("Screenshot uploaded: {0}", url);

            trayIcon.ShowBalloonTip(2000, "Screenshot Uploaded!", url, ToolTipIcon.Info);
        }

        private void UploadScreenshot(string filePath)
        {
            var psResp = _ponyShots.UploadScreenshot(filePath);
            if (psResp.Error)
            {
                HandleError("An error occured on the server side when uploading that screenshot:\r\n" + psResp.ErrorMessage);
            }
            else
            {
                var url = $"{_ponyShots.ImageBaseUrl}{psResp.Slug}";

                Clipboard.SetText(url);
                DisplayUploadedNotification(url);
            }
        }

        private void HandleError(string message)
        {
            MessageBox.Show(message, "PonyShots4Win Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void HandleError(string message, Exception cause)
        {
            HandleError($"{message}\r\n{cause.ToString()}");
        }

        private Screen GetCurrentScreen()
        {
            return Screen.FromPoint(Cursor.Position);
        }

        private void PerformScreenshot(bool shouldCrop)
        {
            var screenshotPath = GenerateFilename();
            var curScreen = GetCurrentScreen();
            var bmpScreenshot = new Bitmap(curScreen.Bounds.Width, curScreen.Bounds.Height, PixelFormat.Format32bppArgb);
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            gfxScreenshot.CopyFromScreen(curScreen.Bounds.X, curScreen.Bounds.Y, 0, 0, curScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
            bmpScreenshot.Save(screenshotPath, ImageFormat.Png);

            if (!shouldCrop)
            {
                UploadScreenshot(screenshotPath);
                return;
            }

            var formDisplayImage = new FormDisplayImage(bmpScreenshot);
            formDisplayImage.SetScreen(curScreen);
            formDisplayImage.ShowDialog();

            if (formDisplayImage.HasSelection)
            {
                var croppedImageName = Path.GetFileNameWithoutExtension(screenshotPath) + "-cropped.png";
                formDisplayImage.SelectedBitmap.Save(croppedImageName, ImageFormat.Png);

                UploadScreenshot(croppedImageName);
            }
        }

        private void PerformScreenshotCurrentWindow()
        {
            var curWnd = WinApi.GetForegroundWindow();

            if (curWnd == IntPtr.Zero)
            {
                HandleError("GetForegroundWindow() == NULL\r\nSomething very bad happened.");
                return;
            }

            RECT myDixieRect;

            if (!WinApi.GetWindowRect(curWnd, out myDixieRect))
            {
                HandleError("GetWindowRect() == FALSE\r\nSomething very bad happened.");
                return;
            }

            var upperLeft = myDixieRect.Location;
            var sz = myDixieRect.Size;
            var bitmap = new Bitmap(sz.Width, sz.Height);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(upperLeft, Point.Empty, sz);
            }

            var screenshotPath = GenerateFilename();
            bitmap.Save(screenshotPath, ImageFormat.Png);
            UploadScreenshot(screenshotPath);
        }

        private void UploadFromClipboard()
        {
            var screenshotPath = GenerateFilename();

            if (Clipboard.ContainsImage())
            {
                var img = Clipboard.GetImage();
                img.Save(screenshotPath + ".png");
                UploadScreenshot(screenshotPath);
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var images = Clipboard.GetFileDropList();

                foreach (var imageFile in images)
                {
                    var ext = Path.GetExtension(imageFile);

                    if (new string[] {".png", ".jpg", ".jpeg", ".gif"}.Contains(ext.ToLower()))
                    {
                        var img = Image.FromFile(imageFile);
                        img.Save(screenshotPath);
                        UploadScreenshot(screenshotPath);
                    }
                }

            }
        }

        private string GenerateFilename()
        {
            var t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var secondsSinceEpoch = (int)t.TotalSeconds;

            return Path.Combine(BASE_PATH, secondsSinceEpoch.ToString() + ".png");
        }

        public void OnHotKey(Keys k, KeyModifier modifier)
        {
            try
            {
                switch (k)
                {
                    case Keys.D2:
                        this.PerformScreenshotCurrentWindow();
                        break;
                    case Keys.D3:
                        this.PerformScreenshot(false);
                        break;
                    case Keys.D4:
                        this.PerformScreenshot(true);
                        break;
                    case Keys.D5:
                        this.UploadFromClipboard();
                        break;
                }
            }
            catch (Exception e)
            {
                HandleError("Error occured taking a screenshot:", e);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            _hotKeys?.WndProc(ref m);
            /*if (m.Msg == 0x0312)
            {
                var key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                var modifier = (KeyModifier)((int)m.LParam & 0xFFFF);              // The modifier of the hotkey that was pressed.
                var id = m.WParam.ToInt32();                                       // The id of the hotkey that was pressed.

                try
                {
                    switch (key)
                    {
                        case Keys.D2:
                            this.PerformScreenshotCurrentWindow();
                            break;
                        case Keys.D3:
                            this.PerformScreenshot(false);
                            break;
                        case Keys.D4:
                            this.PerformScreenshot(true);
                            break;
                        case Keys.D5:
                            this.UploadFromClipboard();
                            break;
                    }
                }
                catch (Exception e)
                {
                    HandleError("Error occured taking a screenshot:", e);
                }
            }*/

        }

        private void openDirBtn_Click(object sender, EventArgs e)
        {
            Process.Start(BASE_PATH);
        }

        private void quitBtn_Click(object sender, EventArgs e)
        {
            this._exiting = true;
            this.Close();
        }

        private void hideBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
