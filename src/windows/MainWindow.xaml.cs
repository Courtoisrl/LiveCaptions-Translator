using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

using LiveCaptionsTranslator.utils;
using LiveCaptionsTranslator.Utils;
using Button = Wpf.Ui.Controls.Button;

namespace LiveCaptionsTranslator
{
    public partial class MainWindow : FluentWindow
    {
        public OverlayWindow? OverlayWindow { get; set; } = null;
        public bool IsAutoHeight { get; set; } = true;

        private bool isHiddenToTray = false;
        private WindowState stateBeforeMinimize = WindowState.Normal;
        private bool startupActionsDone = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            StateChanged += MainWindow_StateChanged;

            // Translation starts paused on every launch; keep the toggle icon in sync.
            if (LogOnlyButton.Icon is SymbolIcon logOnlyIcon)
                logOnlyIcon.Filled = Translator.LogOnlyFlag;

            Loaded += (s, e) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
                RootNavigation.Navigate(typeof(CaptionPage));
                IsAutoHeight = true;
                CheckForFirstUse();
                CheckForUpdates();

                // These startup actions must run exactly once: Loaded fires again when
                // the window is shown after being restored from the notification area.
                if (!startupActionsDone)
                {
                    startupActionsDone = true;

                    // Restore the last overlay state, then hide the main window if requested.
                    if (Translator.Setting.OverlayWindowEnabled)
                        ShowOverlayWindow();

                    // Hide after the window's initial ShowWindow call, otherwise Win32
                    // shows it again and the window stays on screen.
                    if (Translator.Setting.StartMinimized)
                        Dispatcher.BeginInvoke(new Action(MinimizeToTray), DispatcherPriority.ApplicationIdle);
                }
            };

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            var windowState = WindowHandler.LoadState(this, Translator.Setting);
            if (windowState.Left <= 0 || windowState.Left >= screenWidth ||
                windowState.Top <= 0 || windowState.Top >= screenHeight)
            {
                WindowHandler.RestoreState(this, new Rect(
                    (screenWidth - 775) / 2, screenHeight * 3 / 4 - 167, 775, 167));
            }
            else
                WindowHandler.RestoreState(this, windowState);

            ToggleTopmost(Translator.Setting.MainWindow.Topmost);
            ShowLogCard(Translator.Setting.MainWindow.CaptionLogEnabled);
        }

        private void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleTopmost(!this.Topmost);
        }

        private void OverlayModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (OverlayWindow == null)
                ShowOverlayWindow();
            else
                CloseOverlayWindow();
        }

        public void ShowOverlayWindow()
        {
            if (OverlayWindow != null)
                return;

            var symbolIcon = OverlayModeButton.Icon as SymbolIcon;
            if (symbolIcon != null)
            {
                symbolIcon.Symbol = SymbolRegular.ClosedCaption24;
                symbolIcon.Filled = true;
            }

            var overlay = new OverlayWindow();

            // Restore the layout the user left the overlay in; the first launch shows the
            // original caption only (the overlay button cycles through the other modes).
            var layout = Translator.Setting.OverlayWindow;
            overlay.SwitchMode = layout.SwitchMode;
            overlay.OnlyMode = layout.OnlyMode;

            overlay.SizeChanged +=
                (s, e) => WindowHandler.SaveState(overlay, Translator.Setting);
            overlay.LocationChanged +=
                (s, e) => WindowHandler.SaveState(overlay, Translator.Setting);

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            var windowState = WindowHandler.LoadState(overlay, Translator.Setting);
            if (windowState.Left <= 0 || windowState.Left >= screenWidth ||
                windowState.Top <= 0 || windowState.Top >= screenHeight)
            {
                WindowHandler.RestoreState(overlay, new Rect(
                    (screenWidth - 650) / 2, screenHeight * 5 / 6 - 135, 650, 135));
            }
            else
                WindowHandler.RestoreState(overlay, windowState);

            OverlayWindow = overlay;
            overlay.Show();

            Translator.Setting.OverlayWindowEnabled = true;
        }

        public void CloseOverlayWindow()
        {
            var overlay = OverlayWindow;
            if (overlay == null)
                return;

            OverlayWindow = null;

            var symbolIcon = OverlayModeButton.Icon as SymbolIcon;
            if (symbolIcon != null)
            {
                symbolIcon.Symbol = SymbolRegular.ClosedCaptionOff24;
                symbolIcon.Filled = false;
            }

            overlay.Close();

            Translator.Setting.OverlayWindowEnabled = false;
        }

        private void LogOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (Translator.LogOnlyFlag)
            {
                Translator.LogOnlyFlag = false;
                symbolIcon.Filled = false;
            }
            else
            {
                Translator.LogOnlyFlag = true;
                symbolIcon.Filled = true;
            }

            Translator.ClearContexts();
        }

        private void CaptionLogButton_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.MainWindow.CaptionLogEnabled = !Translator.Setting.MainWindow.CaptionLogEnabled;
            ShowLogCard(Translator.Setting.MainWindow.CaptionLogEnabled);
            CaptionPage.Instance?.AutoHeight();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            WindowHandler.SaveState(window, Translator.Setting);
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                MinimizeToTray();
            else
                stateBeforeMinimize = WindowState;
        }

        /// <summary>
        /// Hides the main window and shows its icon in the notification area.
        /// </summary>
        private void MinimizeToTray()
        {
            if (isHiddenToTray)
                return;

            isHiddenToTray = true;

            // Register before hiding so the icon is bound to a valid window handle
            try
            {
                TrayIcon.Register();
            }
            catch
            {
                // Without a notification icon the window would become unreachable,
                // so fall back to an ordinary minimized window.
                isHiddenToTray = false;
                return;
            }

            Hide();
        }

        /// <summary>
        /// Restores the main window from the notification area.
        /// </summary>
        private void RestoreFromTray()
        {
            if (!isHiddenToTray)
                return;

            isHiddenToTray = false;

            try
            {
                TrayIcon.Unregister();
            }
            catch
            {
                // The window must be shown again even if the icon cannot be removed;
                // it is disposed when the application exits.
            }

            // Show first: the window keeps its minimized state while hidden, so the
            // WindowState change below is what actually restores it on screen.
            // Restoring the state before Show() leaves the window minimized once
            // it becomes visible again.
            Show();
            WindowState = stateBeforeMinimize == WindowState.Minimized
                ? WindowState.Normal
                : stateBeforeMinimize;
            Activate();

            // Bring the window to the foreground without altering the "always on top" setting
            bool topmost = Topmost;
            Topmost = true;
            Topmost = topmost;
        }

        private void TrayIcon_LeftClick(object sender, RoutedEventArgs e)
        {
            RestoreFromTray();
        }

        private void TrayShow_Click(object sender, RoutedEventArgs e)
        {
            RestoreFromTray();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            TrayIcon.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            TrayIcon.Dispose();
            base.OnClosed(e);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainWindow_LocationChanged(sender, e);
            IsAutoHeight = false;
        }

        public void ToggleTopmost(bool enabled)
        {
            var button = TopmostButton as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            symbolIcon.Filled = enabled;
            this.Topmost = enabled;
            Translator.Setting.MainWindow.Topmost = enabled;
        }

        private void CheckForFirstUse()
        {
            if (!Translator.FirstUseFlag)
                return;

            RootNavigation.Navigate(typeof(SettingPage));
            LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);

            Dispatcher.InvokeAsync(() =>
            {
                var welcomeWindow = new WelcomeWindow
                {
                    Owner = this
                };
                welcomeWindow.Show();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private async Task CheckForUpdates()
        {
            if (Translator.FirstUseFlag)
                return;

            string latestVersion = string.Empty;
            try
            {
                latestVersion = await UpdateUtil.GetLatestVersion();
            }
            catch (Exception ex)
            {
                SnackbarHost.Show("[ERROR] 检查更新失败。", ex.Message, SnackbarType.Error,
                    timeout: 2, closeButton: true);

                return;
            }

            var currentVersion = UpdateUtil.NormalizeVersion(Assembly.GetExecutingAssembly().GetName().Version?.ToString());
            var ignoredVersion = Translator.Setting.IgnoredUpdateVersion;
            if (!string.IsNullOrEmpty(ignoredVersion) && UpdateUtil.NormalizeVersion(ignoredVersion) == latestVersion)
                return;
            if (!string.IsNullOrEmpty(latestVersion) && latestVersion != currentVersion)
            {
                var dialog = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "发现新版本",
                    Content = $"检测到新版本：{latestVersion}\n" +
                              $"当前版本：{currentVersion}\n" +
                              $"请前往 GitHub 下载最新版本。",
                    PrimaryButtonText = "更新",
                    CloseButtonText = "忽略此版本"
                };
                var result = await dialog.ShowDialogAsync();

                if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    var url = UpdateUtil.GitHubReleasesUrl;
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        SnackbarHost.Show("[ERROR] 打开浏览器失败。", ex.Message, SnackbarType.Error,
                            timeout: 2, closeButton: true);
                    }
                }
                else
                    Translator.Setting.IgnoredUpdateVersion = latestVersion;
            }
        }

        public void ShowLogCard(bool enabled)
        {
            if (CaptionLogButton.Icon is SymbolIcon icon)
            {
                if (enabled)
                    icon.Symbol = SymbolRegular.History24;
                else
                    icon.Symbol = SymbolRegular.HistoryDismiss24;
                CaptionPage.Instance?.CollapseTranslatedCaption(enabled);
            }
        }

        public void AutoHeightAdjust(int minHeight = -1, int maxHeight = -1)
        {
            if (minHeight > 0 && Height < minHeight)
            {
                Height = minHeight;
                IsAutoHeight = true;
            }

            if (IsAutoHeight && maxHeight > 0 && Height > maxHeight)
                Height = maxHeight;
        }
    }
}