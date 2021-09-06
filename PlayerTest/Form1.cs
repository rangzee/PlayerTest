using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using static PlayerTest.DirectShowUtils;
using static PlayerTest.ZeratoolPlayerEngine;

namespace PlayerTest
{
    public partial class Form1 : Form
    {
        private ZeratoolPlayerEngine player;
        private Thread timerThread;
        private string filename;

        public Form1()
        {
            InitializeComponent();
        }

        public Form1(string filename) : this()
        {
            this.filename = filename;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            player = new ZeratoolPlayerEngine();
            player.VideoOutputWindow = panelVideoOutput;
            if (!string.IsNullOrWhiteSpace(this.filename))
            {
                player.FileName = this.filename;
                int errorCode = player.BuildGraph();
                if (errorCode != S_OK)
                {
                    ShowError(errorCode);
                    return;
                }

                player.Pause();

                player.Video.GetVideoSize(out int videoWidth, out int videoHeight);
                this.ClientSize = GetWindowSize(videoWidth, videoHeight);
                taskbarTracker.SetRange(0, (int)player.Duration);

                player.Play();
            }

            timerThread = new Thread(new ThreadStart(updateTimeBarThread));
            timerThread.Start();
        }

        private void updateTimeBarThread()
        {
            while (true)
            {
                var time = player.Position;
                //Console.WriteLine(time);
                //player.Audio.get_Volume(out int vol);
                //Console.WriteLine(vol);
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    taskbarTracker.Value = (int)time;
                }));
                Thread.Sleep(1000);
            }
        }

        private Size GetWindowSize(int videoWidth, int videoHeight)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                return new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            }

            var desktopWidth = Screen.PrimaryScreen.WorkingArea.Width;
            var desktopHeight = Screen.PrimaryScreen.WorkingArea.Height;

            if (taskbarTracker.Visible)
            {
                videoHeight += taskbarTracker.ClientRectangle.Height;
            }

            var ratio = videoHeight / (double)videoWidth;
            var ratioDesktop = desktopHeight / (double)desktopWidth;
            // if video size > desktop size
            if (videoHeight > desktopHeight || videoWidth > desktopWidth)
            {
                // video aspect ratio > desktop aspect ratio, use screen height, center horizontally
                if (ratio > ratioDesktop)
                {
                    var zoomRatio = desktopHeight / (double)videoHeight;
                    videoWidth = (int)(videoWidth * zoomRatio);
                    videoHeight = desktopHeight;
                    this.Top = 0;
                    this.Left = (desktopWidth - videoWidth) / 2;
                }
                else
                {
                    var zoomRatio = desktopWidth / (double)videoWidth;
                    videoWidth = desktopWidth;
                    videoHeight = (int)(videoHeight * zoomRatio);
                    this.Left = 0;
                    this.Top = (desktopHeight - videoHeight) / 2;
                }
            }
            else
            {
                this.Left = (desktopWidth - videoWidth) / 2;
                this.Top = (desktopHeight - videoHeight) / 2;
            }

            return new Size(videoWidth, videoHeight);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (timerThread != null && timerThread.IsAlive)
            {
                timerThread.Abort();
            }

            player.Clear();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (player != null && player.Video != null && panelVideoOutput != null)
            {
                player.Video.GetVideoSize(out int videoWidth, out int videoHeight);
                var ratio = videoHeight / (double)videoWidth;

                var rect = panelVideoOutput.ClientRectangle;
                var ratioRect = rect.Height / (double)rect.Width;

                // video aspect ratio > rect aspect ratio, use rect height, center horizontally
                if (ratio > ratioRect)
                {
                    var zoomRatio = rect.Height / (double)videoHeight;
                    videoWidth = (int)(videoWidth * zoomRatio);

                    rect.X += (rect.Width - videoWidth) / 2;
                    rect.Width = videoWidth;
                }
                // use rect width, center vertically
                else
                {
                    var zoomRatio = rect.Width / (double)videoWidth;
                    videoHeight = (int)(videoHeight * zoomRatio);

                    rect.Y += (rect.Height - videoHeight) / 2;
                    rect.Height = videoHeight;
                }

                player.SetVideoOutputRectangle(rect);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Q:
                    //case Keys.Escape:
                    Close();
                    return;

                case Keys.Enter:
                    this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                    break;

                case Keys.Space:
                    if (player.IsPlaying)
                    {
                        player.Pause();
                    }
                    else
                    {
                        player.Play();
                    }
                    break;

                case Keys.PageDown:
                    {
                        player.Clear();
                        player.FileName = @"D:\2.mp4";
                        player.BuildGraph();
                        player.Pause();
                        player.Video.GetVideoSize(out int videoWidth, out int videoHeight);
                        this.ClientSize = GetWindowSize(videoWidth, videoHeight);
                        taskbarTracker.SetRange(0, (int)player.Duration);
                        player.Play();
                    }
                    break;

                case Keys.PageUp:
                    {
                        player.Clear();
                        player.FileName = @"D:\1.mp4";
                        player.BuildGraph();
                        player.Pause();
                        player.Video.GetVideoSize(out int videoWidth, out int videoHeight);
                        this.ClientSize = GetWindowSize(videoWidth, videoHeight);
                        taskbarTracker.SetRange(0, (int)player.Duration);
                        player.Play();
                    }
                    break;

                case Keys.C:
                    {
                        taskbarTracker.Visible = !taskbarTracker.Visible;
                        player.Pause();
                        player.Video.GetVideoSize(out int videoWidth, out int videoHeight);
                        this.ClientSize = GetWindowSize(videoWidth, videoHeight);
                        taskbarTracker.SetRange(0, (int)player.Duration);
                        player.Play();
                    }
                    break;

                case Keys.R:
                    double pos = player.Position;
                    player.Clear();
                    if (player.BuildGraph() == S_OK && pos > 0.0)
                    {
                        player.Position = pos;
                    }
                    break;
            }
        }

        private void ShowError(int errorCode)
        {
            switch (errorCode)
            {
                case ERROR_FILE_NAME_NOT_DEFINED:
                    MessageBox.Show("File name not defined.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case ERROR_FILE_NOT_FOUND:
                    MessageBox.Show($"File not found: {player.FileName}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case ERROR_VIDEO_OUTPUT_WINDOW_NOT_DEFINED:
                    MessageBox.Show("Video output window not defined", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                default:
                    MessageBox.Show(errorCode.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void taskbarTracker_Scroll(object sender, EventArgs e)
        {
            if (player.MediaPosition != null)
            {
                player.MediaPosition.put_CurrentPosition(taskbarTracker.Value);
            }
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            var result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.filename = ofd.FileName;

                player.Clear();
                player.FileName = filename;
                player.BuildGraph();
                player.Pause();
                player.Video.GetVideoSize(out int videoWidth, out int videoHeight);
                this.ClientSize = GetWindowSize(videoWidth, videoHeight);
                taskbarTracker.SetRange(0, (int)player.Duration);
                player.Play();
            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(MousePosition.X, MousePosition.Y);
            }
        }

        private void panelVideoOutput_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(MousePosition.X, MousePosition.Y);
            }
        }
    }
}
