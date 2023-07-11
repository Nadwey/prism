using prism.Fonts;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace prism
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
        // maybe l8r
        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ChangeProgress(80, 200, "Searching for Minecraft window...");

            DispatcherTimer CaptureWindowTimer = new DispatcherTimer();
            CaptureWindowTimer.Interval = TimeSpan.FromMilliseconds(1);
            CaptureWindowTimer.Tick += CaptureWindow_Tick;

            foreach (Process process in Process.GetProcesses())
            {
                if (!string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    foreach (string allowedClient in Config.AllowedClients)
                    {
                        if (process.MainWindowTitle.StartsWith(allowedClient))
                        {
                            await ChangeProgress(200, 600, $"Found Minecraft window! [{process.MainWindowTitle}]");
                            LocalMemory.GameClient = process;

                            break; // Exit the loop, window found :cool:
                        }
                    }
                }
            }
            CaptureWindowTimer.Start();
            await Task.Delay(200);
            await ChangeProgress(400, 300, $"Captured Client Information! [X: {LocalMemory.ClientX}, Y: {LocalMemory.ClientY}] - [H: {LocalMemory.ClientHeight}, W: {LocalMemory.ClientWidth}]");
            await ChangeProgress(450, 200, $"Checking for latest vs current version");

            bool VersionCheckResult = false;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string RemoteContent = await client.GetStringAsync(Config.VersionServer);
                    double ServerVersion = double.Parse(RemoteContent);

                    if (ServerVersion > Config.Version)
                    {
                        MessageBox.Show("A newer version of prism is avalible. Please upgrade!");
                    }
                    else if (ServerVersion == Config.Version) { VersionCheckResult = true; }
                    else
                    {
                        MessageBox.Show("How the fuck? Your version is newer than the one on server... Nice experiments!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error: {ex}");
                }
            }

            if (VersionCheckResult == true)
            {
                await ChangeProgress(600, 300, $"Version checking result: Passed.");
            }

            // Launch che@t window [TODO]
            this.Hide();
        }

        private async void CaptureWindow_Tick(object sender, EventArgs e)
        {
            RECT clientRect;
            if (GetClientRect(LocalMemory.GameClient.MainWindowHandle, out clientRect))
            {
                POINT screenPoint = new POINT { X = clientRect.Left, Y = clientRect.Top };
                ClientToScreen(LocalMemory.GameClient.MainWindowHandle, ref screenPoint);

                LocalMemory.ClientWidth = clientRect.Right - clientRect.Left;
                LocalMemory.ClientHeight = clientRect.Bottom - clientRect.Top;
                LocalMemory.ClientX = screenPoint.X; LocalMemory.ClientY = screenPoint.Y;
            }
            else
            {
                await ChangeProgress(0, 0, "Failed to get client information. Close this program asap.");
            }
        }

        private async Task ChangeProgress(double targetWidth, int animationLength, string progressText)
        {
            double currentWidth = loadingbar_bar_loader.ActualWidth;
            var tcs = new TaskCompletionSource<bool>();

            DoubleAnimation widthAnimation = new DoubleAnimation();
            widthAnimation.From = currentWidth;
            widthAnimation.To = targetWidth;
            widthAnimation.Duration = TimeSpan.FromMilliseconds(animationLength);
            widthAnimation.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut };

            widthAnimation.Completed += (s, e) => tcs.SetResult(true);

            loadingbar_bar_loader.BeginAnimation(Rectangle.WidthProperty, widthAnimation);
            loadingbar_text.Content = progressText;

            await tcs.Task;
        }
    }
}
