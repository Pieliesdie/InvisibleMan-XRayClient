using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace InvisibleManXRay.Components
{
    using Values;
    using Services;
    using Services.Analytics.Configuration;
    using CustomMessageBox;

    public partial class Config : UserControl
    {
        private Models.Config config;
        
        private Action onSelect;
        private Action onDelete;
        private Func<Window> getServerWindow;
        private Func<string, int> testConnection;
        private Func<string> getLogPath;

        private BackgroundWorker checkConnectionWorker;

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public Config()
        {
            InitializeComponent();
            InitializeCheckConnectionWorker();

            void InitializeCheckConnectionWorker()
            {
                checkConnectionWorker = new BackgroundWorker();
                checkConnectionWorker.DoWork += (sender, e) => {
                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowLoadingProgress();
                    }));

                    int availability = testConnection.Invoke(config.Path);

                    Dispatcher.BeginInvoke(new Action(delegate {
                        HandleConfigStatus(availability);
                        ShowCheckButton();
                    }));
                };
            }
        }

        public void Setup(
            Models.Config config, 
            Action onSelect, 
            Action onDelete, 
            Func<Window> getServerWindow,
            Func<string, int> testConnection,
            Func<string> getLogPath)
        {
            this.config = config;
            this.onSelect = onSelect;
            this.onDelete = onDelete;
            this.getServerWindow = getServerWindow;
            this.testConnection = testConnection;
            this.getLogPath = getLogPath;

            UpdateUI();
        }

        public void SetSelection(bool isSelect)
        {
            Visibility visibility = isSelect ? Visibility.Visible : Visibility.Hidden;
            gridSelect.Visibility = visibility;
        }

        private void UpdateUI()
        {
            textConfigName.Content = config.Name;
            textUpdateTime.Content = config.UpdateTime;
            
            HandleConfigStatus(config.Availability);
        }

        private void OnSelectButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new SelectButtonClickedEvent());
            onSelect.Invoke();
        }

        private void OnEditButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new EditButtonClickedEvent());

            if (!File.Exists(config.Path))
            {
                MessageBoxCustom.Show(
                    getServerWindow.Invoke(),
                    Message.FILE_DOESNT_EXISTS,
                    Caption.ERROR,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            OpenFileInTextEditor();

            void OpenFileInTextEditor()
            {
                Process fileOpenProcess = new Process();
                fileOpenProcess.StartInfo.FileName = "notepad";
                fileOpenProcess.StartInfo.Arguments = $"\"{config.Path}\"";
                fileOpenProcess.Start();
            }
        }

        private void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new DeleteButtonClickedEvent());

            var result = MessageBoxCustom.Show(
                getServerWindow.Invoke(),
                string.Format(Message.DELETE_CONFIRMATION, config.Name),
                Caption.INFO,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == true)
                DeleteFile();
            
            void DeleteFile()
            {
                try
                {
                    File.Delete(config.Path);
                }
                catch(Exception ex)
                {
                    MessageBoxCustom.Show(getServerWindow.Invoke(), ex.Message);
                }
                finally
                {
                    onDelete.Invoke();
                }
            }
        }

        private void OnCheckButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new CheckButtonClickedEvent());
            checkConnectionWorker.RunWorkerAsync();
        }

        private void OnLogButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new LogButtonClickedEvent());
            string path = System.IO.Path.GetFullPath($"{getLogPath.Invoke()}/{config.Name}");
            
            if (!IsLogDirectoryExists())
            {
                HandleNoLogMessage();
                return;
            }

            OpenLogDirectory();
            

            bool IsLogDirectoryExists() => System.IO.Path.Exists(path);

            void HandleNoLogMessage()
            {
                MessageBoxCustom.Show(
                    getServerWindow.Invoke(),
                    Message.NO_LOG_FILE,
                    Caption.INFO,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }

            void OpenLogDirectory()
            {
                Process.Start(new ProcessStartInfo() {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }

        private void HandleConfigStatus(int availability)
        {
            config.SetAvailability(availability);

            switch(availability)
            {
                case Availability.NOT_CHECKED:
                    ShowNotCheckedStatus();
                    break;
                case Availability.TIMEOUT or Availability.ERROR:
                    ShowTimeoutStatus();
                    break;
                default:
                    ShowAvailableStatus();
                    break;
            }
        }

        private void ShowNotCheckedStatus()
        {
            statusNotChecked.Visibility = Visibility.Visible;
            statusAvailable.Visibility = Visibility.Hidden;
            statusTimeout.Visibility = Visibility.Hidden;
        }

        private void ShowTimeoutStatus()
        {
            statusTimeout.Visibility = Visibility.Visible;
            statusNotChecked.Visibility = Visibility.Hidden;
            statusAvailable.Visibility = Visibility.Hidden;
        }

        private void ShowAvailableStatus()
        {
            statusAvailable.Visibility = Visibility.Visible;
            statusNotChecked.Visibility = Visibility.Hidden;
            statusTimeout.Visibility = Visibility.Hidden;

            textAvailability.Content = $"{config.Availability} ms";
        }

        private void ShowLoadingProgress()
        {
            progressLoading.Visibility = Visibility.Visible;
            buttonCheck.Visibility = Visibility.Collapsed;
        }

        private void ShowCheckButton()
        {
            buttonCheck.Visibility = Visibility.Visible;
            progressLoading.Visibility = Visibility.Collapsed;
        }
    }
}
