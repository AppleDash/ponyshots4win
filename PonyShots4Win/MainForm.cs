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

namespace PonyShots4Win
{
    public partial class MainForm : Form
    {
        private static string BASE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "PonyShots");
        private static string CONFIG_FILE = Path.Combine(BASE_PATH, "ponyshots4win.json");
        private bool exiting = false;
        private PonyShots ponyShots;
        private Config config;

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
                    config = Config.Parse(CONFIG_FILE);
                }
                catch (Exception e)
                {
                    HandleError("Failed to parse config, loading defaults:", e);
                    config = Config.Default();
                    config.Save(CONFIG_FILE);
                }
            }
            else
            {
                config = Config.Default();
                config.Save(CONFIG_FILE);
                MessageBox.Show("You had no config; I created a default one for you.\r\nYou probably want to edit '" + CONFIG_FILE + "'.", "PonyShots4Win");
            }

            ponyShots = new PonyShots();
            ponyShots.UploadUrl = config.UploadUrl;
            ponyShots.ImageBaseUrl = config.BaseUrl;
            ponyShots.Username = config.Username;
            ponyShots.ApiKey = config.ApiKey;
            WinApi.RegisterHotKey(this.Handle, 0, ((int)KeyModifier.Control | (int)KeyModifier.Shift), Keys.D4.GetHashCode());
            WinApi.RegisterHotKey(this.Handle, 1, ((int)KeyModifier.Control | (int)KeyModifier.Shift), Keys.D2.GetHashCode());

        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exiting = true;
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.exiting)
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
            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.BalloonTipTitle = "Screenshot Uploaded!";
            notifyIcon.BalloonTipText = string.Format("Screenshot uploaded: {0}", url);
            notifyIcon.ShowBalloonTip(2000);
        }

        private void UploadScreenshot(string filePath)
        {
            PonyShotsResponse psResp = ponyShots.UploadScreenshot(filePath);
            if (psResp.Error)
            {
                HandleError("An error occured on the server side when uploading that screenshot:\r\n" + psResp.ErrorMessage);
            }
            else
            {
                string url = string.Format("{0}{1}", ponyShots.ImageBaseUrl, psResp.Slug);
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
            HandleError(string.Format("{0}\r\n{1}", message, cause.ToString()));
        }

        private void PerformScreenshot()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)t.TotalSeconds;
            string screenshotPath = Path.Combine(BASE_PATH, secondsSinceEpoch.ToString()); 
            Bitmap bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
            Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot);
            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
            bmpScreenshot.Save(screenshotPath + ".png", ImageFormat.Png);

            FormDisplayImage formDisplayImage = new FormDisplayImage(bmpScreenshot);
            formDisplayImage.ShowDialog();
            if (formDisplayImage.HasSelection)
            {
                string croppedImageName = screenshotPath + "-cropped.png";
                formDisplayImage.SelectedBitmap.Save(croppedImageName, ImageFormat.Png);

                UploadScreenshot(croppedImageName);
            }
        }

        private void PerformScreenshotCurrentWindow()
        {
            IntPtr curWnd = WinApi.GetForegroundWindow();

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

            Point upperLeft = myDixieRect.Location;
            Size sz = myDixieRect.Size;
            Bitmap bitmap = new Bitmap(sz.Width, sz.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(upperLeft, Point.Empty, sz);
            }

            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)t.TotalSeconds;
            string screenshotPath = Path.Combine(BASE_PATH, secondsSinceEpoch.ToString());

            bitmap.Save(screenshotPath + ".png", ImageFormat.Png);
            UploadScreenshot(screenshotPath + ".png");
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312)
            {
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.


                if (key == Keys.D4)
                {
                    try
                    {
                        this.PerformScreenshot();
                    }
                    catch (Exception e)
                    {
                        HandleError("Error occured taking a cropped screenshot:", e);
                    }
                }
                else if (key == Keys.D2)
                {
                    try
                    {
                        this.PerformScreenshotCurrentWindow();
                    }
                    catch (Exception e)
                    {
                        HandleError("Error occured taking a window screenshot:", e);
                    }
                }
            }
        }

        private void openDirBtn_Click(object sender, EventArgs e)
        {
            Process.Start(BASE_PATH);
        }

        private void quitBtn_Click(object sender, EventArgs e)
        {
            this.exiting = true;
            this.Close();
        }
    }
}
