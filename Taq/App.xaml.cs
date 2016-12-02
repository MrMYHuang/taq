using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TaqShared;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Taq
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public Shared shared = new Shared();
        public Windows.Storage.ApplicationDataContainer localSettings;

        public App()
        {
            initLocalSettings();
            shared.loadSiteGeoXd();
            // Fox Xbox One gamepad XY focus navigation. Not tested.
            //this.RequiresPointerMode =
            //Windows.UI.Xaml.ApplicationRequiresPointerMode.WhenRequested;
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += (sender, e) =>
            {
                // Comment the following line for exiting app.
                //e.Handled = true;
                Launcher.LaunchUriAsync(new Uri("mailto:myhDev@live.com?subject=TAQ%20App異常回報&body=請寄送以下app異常訊息給開發者，謝謝。%0D%0A版本：" + version + "%0D%0A例外：" + e.Exception.ToString() + "%0D%0A若能提供其他造成異常的資訊，可使開發者更快找出問題，謝謝。"));
            };
            //this.RegisterBackgroundTask();
        }

        private void initLocalSettings()
        {
            localSettings =
       Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values["AppTheme"] == null)
            {
                localSettings.Values["AppTheme"] = false;
            }
            this.RequestedTheme = (bool)localSettings.Values["AppTheme"] ? ApplicationTheme.Dark : ApplicationTheme.Light;
            if (localSettings.Values["MapColor"] == null)
            {
                localSettings.Values["MapColor"] = false;
            }
            if (localSettings.Values["Aqi_LimitId"] == null)
            {
                localSettings.Values["Aqi_LimitId"] = 50;
            }
            if (localSettings.Values["Pm2_5_LimitId"] == null)
            {
                localSettings.Values["Pm2_5_LimitId"] = 3;
            }
            if (localSettings.Values["subscrSite"] == null)
            {
                localSettings.Values["subscrSite"] = "中壢";
            }
            if (localSettings.Values["BgUpdatePeriod"] == null)
            {
                BgUpdatePeriodId = 2;
            }
            else
            {
                BgUpdatePeriodId = bgUpdatePeriods.FindIndex(x => x == (int)localSettings.Values["BgUpdatePeriod"]);
            }
        }

        private int bgUpdatePeriodId;
        public int BgUpdatePeriodId
        {
            get
            {
                return bgUpdatePeriodId;
            }
            set
            {
                bgUpdatePeriodId = value;
                localSettings.Values["BgUpdatePeriod"] = bgUpdatePeriods[value];
                RegisterBackgroundTask();
                NotifyPropertyChanged();
            }
        }

        public List<int> bgUpdatePeriods = new List<int> { 15, 20, 30, 60 };

        private const string taskName = "TaqBackTask";
        private const string taskEntryPoint = "TaqBackTask.TaqBackTask";
        private async void RegisterBackgroundTask()
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy ||
                backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed)
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                    }
                }

                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;
                taskBuilder.SetTrigger(new TimeTrigger(Convert.ToUInt32(localSettings.Values["BgUpdatePeriod"]), false));
                var registration = taskBuilder.Register();
            }
        }

        public string version
        {
            get
            {
                return String.Format("{0}.{1}.{2}",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Build);
            }
        }

        public bool MapColor
        {
            get
            {
                return (bool)localSettings.Values["MapColor"];
            }

            set
            {
                localSettings.Values["MapColor"] = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        protected override void OnActivated(IActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage));
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
