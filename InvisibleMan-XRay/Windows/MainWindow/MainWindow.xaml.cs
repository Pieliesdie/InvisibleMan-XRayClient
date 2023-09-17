using System;
using System.ComponentModel;
using System.Windows;

namespace InvisibleManXRay
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media;

    using CustomMessageBox;

    using InvisibleManXRay.Windows;

    using LiveChartsCore;
    using LiveChartsCore.SkiaSharpView;

    using MaterialDesignThemes.Wpf;

    using Microsoft.Toolkit.Uwp.Notifications;

    using Models;

    using Services;
    using Services.Analytics.General;
    using Services.Analytics.MainWindow;

    using Values;

    public partial class MainWindow : BaseWindow
    {
        private bool isRerunRequest;
        private Func<Config> getConfig;
        private Func<Status> loadConfig;
        private Func<Status> enableMode;
        private Func<Status> checkForUpdate;
        private Func<ServerWindow> openServerWindow;
        private Func<SettingsWindow> openSettingsWindow;
        private Func<UpdateWindow> openUpdateWindow;
        private Func<AboutWindow> openAboutWindow;
        private Action<string> onRunServer;
        private Action onCancelServer;
        private Action onStopServer;
        private Action onDisableMode;
        private Action onGitHubClick;
        private Action onBugReportingClick;

        private BackgroundWorker runWorker;
        private BackgroundWorker updateWorker;
        private static NetworkInterface NetworkInterface => NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.Name.Contains("XRay"));
        private static AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public List<float> DownloadSpeedSeries { get; set; } = new();
        public List<float> UploadSpeedSeries { get; set; } = new();

        public ObservableCollection<ISeries> SpeedSeries { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitializeRunWorker();
            InitializeUpdateWorker();

            updateWorker.RunWorkerAsync();

            void InitializeRunWorker()
            {
                runWorker = new BackgroundWorker();

                runWorker.RunWorkerCompleted += (sender, e) =>
                {
                    if (isRerunRequest)
                    {
                        runWorker.RunWorkerAsync();
                        isRerunRequest = false;
                    }
                };

                runWorker.DoWork += (sender, e) =>
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        ShowWaitForRunStatus();
                    }));

                    Status configStatus = loadConfig.Invoke();

                    if (configStatus.Code == Code.ERROR)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            HandleError();
                            ShowStopStatus(false);
                        }));

                        return;
                    }

                    Status modeStatus = enableMode.Invoke();

                    if (modeStatus.Code == Code.ERROR)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            MessageBoxCustom.Show(
                                this,
                                modeStatus.Content.ToString(),
                                Caption.ERROR,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                            ShowStopStatus();
                        }));

                        return;
                    }
                    else if (modeStatus.Code == Code.INFO)
                    {
                        if (modeStatus.SubCode == SubCode.CANCELED)
                        {
                            Dispatcher.BeginInvoke(new Action(delegate
                            {
                                ShowStopStatus();
                            }));

                            return;
                        }
                    }

                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        ShowRunStatus();
                    }));

                    onRunServer.Invoke(configStatus.Content.ToString());

                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        ShowStopStatus();
                    }));

                    void HandleError()
                    {
                        if (IsAnotherWindowOpened())
                            return;

                        switch (configStatus.SubCode)
                        {
                            case SubCode.NO_CONFIG:
                                HandleNoConfigError();
                                break;
                            case SubCode.INVALID_CONFIG:
                                HandleInvalidConfigError();
                                break;
                            default:
                                return;
                        }
#if DEBUG
                        bool IsAnotherWindowOpened() => Application.Current.Windows.Count > 2;
#else
                        bool IsAnotherWindowOpened() => Application.Current.Windows.Count > 1;
#endif
                        void HandleNoConfigError()
                        {
                            var result = MessageBoxCustom.Show(
                                this,
                                configStatus.Content.ToString(),
                                "",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            if (result == true)
                                OpenServerWindow();

                            var config = getConfig();
                            if (config != null)
                            {
                                Run();
                            }

                        }

                        void HandleInvalidConfigError()
                        {
                            MessageBoxCustom.Show(
                                this,
                                configStatus.Content.ToString(),
                                Caption.ERROR,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                        }
                    }
                };
            }

            void InitializeUpdateWorker()
            {
                updateWorker = new BackgroundWorker();

                updateWorker.DoWork += (sender, e) =>
                {
                    Status updateStatus = checkForUpdate.Invoke();
                    if (IsUpdateAvailable())
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            notificationUpdate.Visibility = Visibility.Visible;
                        }));

                    bool IsUpdateAvailable() => updateStatus.SubCode == SubCode.UPDATE_AVAILABLE;
                };
            }
        }

        public void Setup(
            Func<bool> isNeedToShowPolicyWindow,
            Func<Config> getConfig,
            Func<Status> loadConfig,
            Func<Status> enableMode,
            Func<Status> checkForUpdate,
            Func<ServerWindow> openServerWindow,
            Func<SettingsWindow> openSettingsWindow,
            Func<UpdateWindow> openUpdateWindow,
            Func<AboutWindow> openAboutWindow,
            Func<PolicyWindow> openPolicyWindow,
            Action<string> onRunServer,
            Action onStopServer,
            Action onCancelServer,
            Action onDisableMode,
            Action onGenerateClientId,
            Action onGitHubClick,
            Action onBugReportingClick,
            Action<string> onCustomLinkClick)
        {
            this.getConfig = getConfig;
            this.loadConfig = loadConfig;
            this.checkForUpdate = checkForUpdate;
            this.openServerWindow = openServerWindow;
            this.openSettingsWindow = openSettingsWindow;
            this.openUpdateWindow = openUpdateWindow;
            this.openAboutWindow = openAboutWindow;
            this.onRunServer = onRunServer;
            this.onCancelServer = onCancelServer;
            this.onStopServer = onStopServer;
            this.enableMode = enableMode;
            this.onDisableMode = onDisableMode;
            this.onGitHubClick = onGitHubClick;
            this.onBugReportingClick = onBugReportingClick;

            UpdateUI();
        }

        public void UpdateUI()
        {
            Config config = getConfig.Invoke();

            if (config == null)
            {
                textServerConfig.Text = Message.NO_SERVER_CONFIGURATION;
                return;
            }

            textServerConfig.Text = config.Name;
        }

        public void TryRerun()
        {
            if (!runWorker.IsBusy)
                return;

            onStopServer.Invoke();
            isRerunRequest = true;
        }

        public void TryDisableModeAndRerun()
        {
            if (!runWorker.IsBusy)
                return;

            onDisableMode.Invoke();
            onStopServer.Invoke();
            isRerunRequest = true;
        }

        public void Run()
        {
            if (runWorker.IsBusy)
                return;

            runWorker.RunWorkerAsync();

            AnalyticsService.SendEvent(new RunButtonClickedEvent());
        }

        public void Stop()
        {
            onStopServer.Invoke();
            onDisableMode.Invoke();
            isRerunRequest = false;

            AnalyticsService.SendEvent(new StopButtonClickedEvent());
        }

        private void OnManageServersClick(object sender, RoutedEventArgs e)
        {
            OpenServerWindow();
            AnalyticsService.SendEvent(new ManageServersButtonClickedEvent());
        }

        private void OnRunButtonClick(object sender, RoutedEventArgs e)
        {
            Run();
        }

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            if (!runWorker.IsBusy)
                return;

            onCancelServer.Invoke();
        }

        private void OnGitHubButtonClick(object sender, RoutedEventArgs e)
        {
            onGitHubClick.Invoke();
            AnalyticsService.SendEvent(new GitHubButtonClickedEvent());
        }

        private void OnBugReportingButtonClick(object sender, RoutedEventArgs e)
        {
            onBugReportingClick.Invoke();
            AnalyticsService.SendEvent(new BugReportingButtonClickedEvent());
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
            AnalyticsService.SendEvent(new SettingsButtonClickedEvent());
        }

        private void OnUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            OpenUpdateWindow();
            AnalyticsService.SendEvent(new UpdateButtonClickedEvent());
        }

        private void OnAboutButtonClick(object sender, RoutedEventArgs e)
        {
            OpenAboutWindow();
            AnalyticsService.SendEvent(new AboutButtonClickedEvent());
        }

        private void OpenServerWindow()
        {
            this.Hide();
            ServerWindow serverWindow = openServerWindow.Invoke();
            serverWindow.Owner = this;
            serverWindow.ShowDialog();
            this.Show();
        }

        private void OpenSettingsWindow()
        {
            this.Hide();
            SettingsWindow settingsWindow = openSettingsWindow.Invoke();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            this.Show();
        }

        private void OpenUpdateWindow()
        {
            UpdateWindow updateWindow = openUpdateWindow.Invoke();
            updateWindow.Owner = this;
            updateWindow.ShowDialog();
        }

        private void OpenAboutWindow()
        {
            AboutWindow aboutWindow = openAboutWindow.Invoke();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }
        CancellationTokenSource chartCancellationTokenSource = new();
        CancellationToken chartToken;
        private void ShowRunStatus()
        {
            statusRun.Visibility = Visibility.Visible;
            statusStop.Visibility = Visibility.Hidden;
            statusWaitForRun.Visibility = Visibility.Hidden;

            buttonStop.Visibility = Visibility.Visible;
            buttonCancel.Visibility = Visibility.Hidden;
            buttonRun.Visibility = Visibility.Hidden;
            notifyWindow.IconSource = FindResource("AppIconOnTray") as ImageSource;
            ConnectTrayButton.Visibility = Visibility.Collapsed;
            DisconnectTrayButton.Visibility = Visibility.Visible;

            ToastNotificationManagerCompat.History.Clear();
            var config = getConfig();
            new ToastContentBuilder()
                .AddArgument("action", "viewConversation")
                .AddArgument("conversationId", 9813)
                .AddText($"{config.Name.Replace(".json", "")} - connected")
                .SetToastDuration(ToastDuration.Short)
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTime.Now.AddSeconds(5);
                });
            chartCancellationTokenSource.Cancel();
            chartCancellationTokenSource = new();
            chartToken = chartCancellationTokenSource.Token;
            var task = Task.Run(async () =>
            {
                var retryCount = 5;
                while (NetworkInterface is null)
                {
                    await Task.Delay(1000);

                    retryCount--;
                    if (retryCount == 0)
                        return;
                }
                SpeedSeries = new ObservableCollection<ISeries>()
                {
                    new LineSeries<float>()
                    {
                        Values = DownloadSpeedSeries,
                        Fill = null, GeometryFill = null, GeometryStroke = null
                    },
                    new LineSeries<float>()
                    {
                        Values = UploadSpeedSeries,
                        Fill = null, GeometryFill = null, GeometryStroke = null
                    }
                };
                var nic = NetworkInterface;
                var reads = Enumerable.Empty<double>();
                var sw = new Stopwatch();
                var lastBr = nic.GetIPv4Statistics().BytesReceived;
                while (true)
                {
                    if (chartToken.IsCancellationRequested) break;
                    sw.Restart();
                    await Task.Delay(1000);
                    var elapsed = sw.Elapsed.TotalSeconds;
                    var br = nic.GetIPv4Statistics().BytesReceived;

                    var local = (br - lastBr) / elapsed;
                    lastBr = br;

                    // Keep last 20, ~2 seconds
                    reads = new[] { local }.Concat(reads).Take(20);

                    var bSec = reads.Sum() / reads.Count();
                    var kbs = (bSec * 8) / 1024;
                    DownloadSpeedSeries.Add((float)kbs);
                }
            }, chartToken);
        }

        private void ShowStopStatus(bool disconnected = true)
        {
            statusStop.Visibility = Visibility.Visible;
            statusRun.Visibility = Visibility.Hidden;
            statusWaitForRun.Visibility = Visibility.Hidden;

            buttonRun.Visibility = Visibility.Visible;
            buttonCancel.Visibility = Visibility.Hidden;
            buttonStop.Visibility = Visibility.Hidden;
            notifyWindow.IconSource = FindResource("AppIconOffTray") as ImageSource;
            ConnectTrayButton.Visibility = Visibility.Visible;
            DisconnectTrayButton.Visibility = Visibility.Collapsed;

            if (disconnected)
            {
                ToastNotificationManagerCompat.History.Clear();
                new ToastContentBuilder()
                    .AddArgument("action", "viewConversation")
                    .AddArgument("conversationId", 9813)
                    .AddText($"Disconnected")
                    .SetToastDuration(ToastDuration.Short)
                    .Show(toast =>
                    {
                        toast.ExpirationTime = DateTime.Now.AddSeconds(5);
                    });
            }
        }

        private void ShowWaitForRunStatus()
        {
            statusWaitForRun.Visibility = Visibility.Visible;
            statusStop.Visibility = Visibility.Hidden;
            statusRun.Visibility = Visibility.Hidden;

            buttonCancel.Visibility = Visibility.Visible;
            buttonRun.Visibility = Visibility.Hidden;
            buttonStop.Visibility = Visibility.Hidden;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            onDisableMode();
            Application.Current.Shutdown();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            Application.Current.MainWindow.WindowState = WindowState.Normal;
            this.Activate();
        }
    }
}
