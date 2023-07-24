using prism.Fonts;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace prism
{

    public partial class MainWindow : Window
    {
        Windows.MessageBox LocalMessageBox = new Windows.MessageBox();
        AppWindow appwindow_frame = new AppWindow();
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

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
            await ChangeProgress(80, 200, "Searching for Minecraft...");

            DispatcherTimer CaptureWindowTimer = new DispatcherTimer();
            CaptureWindowTimer.Interval = TimeSpan.FromMilliseconds(1);
            CaptureWindowTimer.Tick += CaptureWindow_Tick;

            int FoundClients = 0;
            foreach (Process process in Process.GetProcesses())
            {
                if (!string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    foreach (string allowedClient in Config.AllowedClients)
                    {
                        if (process.MainWindowTitle.StartsWith(allowedClient))
                        {
                            if (process.ProcessName.ToString().Contains("java"))
                            {
                                FoundClients++;

                                await ChangeProgress(200, 600, $"Found Minecraft! [{process.MainWindowTitle}]");
                                LocalMemory.GameClient = process;

                                break; // Exit the loop, window found :cool:
                            }
                        }
                    }
                }
            }

            if (FoundClients == 0)
            {
                LocalMessageBox.ShowMessage("Minecraft client not found. Client not supported or isnt running.");
                System.Environment.Exit(0);
            }

            if (FoundClients > 1)
            {
                LocalMessageBox.ShowMessage("Too many minecraft clients are running at same time. Please use only one at the time.");
                this.Close();
            }

            CaptureWindowTimer.Start();
            await Task.Delay(200);

            // Discord RPC
            await ChangeProgress(250, 200, "Setting up discord RPC.");


            await ChangeProgress(450, 200, $"Checking for latest vs current version");

            bool VersionCheckResult = false;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string RemoteContent = await client.GetStringAsync(Config.VersionServer);
                    double ServerVersion = double.Parse(RemoteContent, CultureInfo.InvariantCulture);

                    if (ServerVersion > Config.Version)
                    {
                        LocalMessageBox.ShowMessage("A never version of prism is avalible. Please update!");
                        System.Environment.Exit(0);
                    }
                    else if (ServerVersion == Config.Version) { VersionCheckResult = true; }
                    else
                    {
                        LocalMessageBox.ShowMessage("How the fuck? Your version is newer than the one on server... Nice experiments, or just a server error lol");
                        System.Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    LocalMessageBox.ShowMessage($"Unexpected error while communicating with version server: {ex}");
                    this.Close();
                }
            }

            if (VersionCheckResult == true)
                await ChangeProgress(600, 300, $"Version checking result: Passed.");
            LocalMemory.GameClient_DefaultTitle = LocalMemory.GameClient.MainWindowTitle;

            ConfigManager.EnsureConfigFileExists();

            if (ConfigManager.GetKey("agreement") == "false")
                appwindow_frame.handlerframe.Navigate(new Windows.UserAgreement.Intro());

            this.Hide();
            appwindow_frame.Show();

            // Focus the LocalMemory.GameClient
            if (LocalMemory.GameClient == null) throw new ArgumentNullException(nameof(LocalMemory.GameClient));
            IntPtr handle = LocalMemory.GameClient.MainWindowHandle;
            if (handle != IntPtr.Zero)
                SetForegroundWindow(handle);
        }

        bool GameClientActive = true;
        bool GameCleintActiveMirror = true;
        private async void CaptureWindow_Tick(object sender, EventArgs e)
        {
            GameCleintActiveMirror = GameClientActive;
            if (IsProcessWindowFocused(LocalMemory.GameClient))
            {
                RECT clientRect;
                if (GetClientRect(LocalMemory.GameClient.MainWindowHandle, out clientRect))
                {
                    POINT screenPoint = new POINT { X = clientRect.Left, Y = clientRect.Top };
                    ClientToScreen(LocalMemory.GameClient.MainWindowHandle, ref screenPoint);

                    //LocalMemory.ClientWidth = clientRect.Right - clientRect.Left;
                    //LocalMemory.ClientHeight = clientRect.Bottom - clientRect.Top;
                    //LocalMemory.ClientX = screenPoint.X; LocalMemory.ClientY = screenPoint.Y;

                    appwindow_frame.Top = screenPoint.Y;
                    appwindow_frame.Left = screenPoint.X;
                    appwindow_frame.Height = clientRect.Bottom - clientRect.Top;
                    appwindow_frame.Width = clientRect.Right - clientRect.Left;

                    if (GameClientActive != true)
                        GameClientActive = true;
                }
                else
                {
                    LocalMessageBox.ShowMessage("Failed to get client information. Closing prism as soon as possible.");
                    System.Environment.Exit(0);
                }
            } else 
            { 
                if (GameClientActive != false)
                    GameClientActive = false;
            }

            if (GameCleintActiveMirror != GameClientActive)
                if (GameClientActive == true)
                {
                    appwindow_frame.Show();
                } else
                {
                    appwindow_frame.Hide();
                }
        }

        private static bool IsProcessWindowFocused(Process process)
        {
            if (process == null)
                return false;

            IntPtr handle = process.MainWindowHandle;
            if (handle != IntPtr.Zero)
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                int currentProcessId;
                GetWindowThreadProcessId(foregroundWindow, out currentProcessId);

                return currentProcessId == process.Id;
            }

            return false;
        }

        public static bool IsProcessWindowVisible(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            IntPtr handle = process.MainWindowHandle;
            if (handle != IntPtr.Zero)
            {
                return IsWindowVisible(handle);
            }

            return false;
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
