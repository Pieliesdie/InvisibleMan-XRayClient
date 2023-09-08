using System;
using System.ComponentModel;
using System.Windows;

namespace InvisibleManXRay
{
    using System.Windows.Media;

    using InvisibleManXRay.Windows;

    using Microsoft.Toolkit.Uwp.Notifications;

    using Models;

    using Services;
    using Services.Analytics.General;
    using Services.Analytics.MainWindow;

    using Values;

    public partial class MainWindow : BaseWindow
    {
        private bool isRerunRequest;
        private Func<bool> isNeedToShowPolicyWindow;
        private Func<Config> getConfig;
        private Func<Status> loadConfig;
        private Func<Status> enableMode;
        private Func<Status> checkForUpdate;
        private Func<ServerWindow> openServerWindow;
        private Func<SettingsWindow> openSettingsWindow;
        private Func<UpdateWindow> openUpdateWindow;
        private Func<AboutWindow> openAboutWindow;
        private Func<PolicyWindow> openPolicyWindow;
        private Action<string> onRunServer;
        private Action onCancelServer;
        private Action onStopServer;
        private Action onDisableMode;
        private Action onGenerateClientId;
        private Action onGitHubClick;
        private Action onBugReportingClick;

        private BackgroundWorker runWorker;
        private BackgroundWorker updateWorker;

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

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
                            ShowStopStatus();
                        }));

                        return;
                    }

                    Status modeStatus = enableMode.Invoke();

                    if (modeStatus.Code == Code.ERROR)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            MessageBox.Show(
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

                        bool IsAnotherWindowOpened() => Application.Current.Windows.Count > 1;

                        void HandleNoConfigError()
                        {
                            MessageBoxResult result = MessageBox.Show(
                                this,
                                configStatus.Content.ToString(),
                                Caption.WARNING,
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );

                            if (result == MessageBoxResult.OK)
                                OpenServerWindow();
                        }

                        void HandleInvalidConfigError()
                        {
                            MessageBox.Show(
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
            this.isNeedToShowPolicyWindow = isNeedToShowPolicyWindow;
            this.getConfig = getConfig;
            this.loadConfig = loadConfig;
            this.checkForUpdate = checkForUpdate;
            this.openServerWindow = openServerWindow;
            this.openSettingsWindow = openSettingsWindow;
            this.openUpdateWindow = openUpdateWindow;
            this.openAboutWindow = openAboutWindow;
            this.openPolicyWindow = openPolicyWindow;
            this.onRunServer = onRunServer;
            this.onCancelServer = onCancelServer;
            this.onStopServer = onStopServer;
            this.enableMode = enableMode;
            this.onDisableMode = onDisableMode;
            this.onGenerateClientId = onGenerateClientId;
            this.onGitHubClick = onGitHubClick;
            this.onBugReportingClick = onBugReportingClick;

            UpdateUI();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            // TryOpenPolicyWindow();
            // AnalyticsService.SendEvent(new AppOpenedEvent());
        }

        public void UpdateUI()
        {
            Config config = getConfig.Invoke();

            if (config == null)
            {
                textServerConfig.Content = Message.NO_SERVER_CONFIGURATION;
                return;
            }

            textServerConfig.Content = config.Name;
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

        private void TryOpenPolicyWindow()
        {
            if (!isNeedToShowPolicyWindow.Invoke())
                return;

            onGenerateClientId.Invoke();
            AnalyticsService.SendEvent(new NewUserEvent());

            PolicyWindow policyWindow = openPolicyWindow.Invoke();
            policyWindow.Owner = this;
            policyWindow.ShowDialog();
        }

        private void OpenServerWindow()
        {
            ServerWindow serverWindow = openServerWindow.Invoke();
            serverWindow.Owner = this;
            serverWindow.ShowDialog();
        }

        private void OpenSettingsWindow()
        {
            SettingsWindow settingsWindow = openSettingsWindow.Invoke();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
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

            var config = getConfig();
            new ToastContentBuilder()
                .AddArgument("action", "viewConversation")
                .AddArgument("conversationId", 9813)
                .AddText($"{config.Name.Replace(".json", "")} Connected")
                .SetToastDuration(ToastDuration.Short)
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTime.Now.AddSeconds(5);
                });
        }

        private void ShowStopStatus()
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
