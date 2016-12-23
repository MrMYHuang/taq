﻿using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using Windows.Storage;
using System.Threading.Tasks;
using Taq.Views;

namespace Taq
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public TaqViewModel vm = new TaqViewModel();
        // Some app settings are stored in LocalSettings.
        public ApplicationDataContainer localSettings;

        public App()
        {
            initLocalSettings();
            // Fox Xbox One gamepad XY focus navigation. Not tested.
            //this.RequiresPointerMode =
            //Windows.UI.Xaml.ApplicationRequiresPointerMode.WhenRequested;
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            // Crash feedback mail report mechanism.
            this.UnhandledException += async (sender, e) =>
            {
                // Comment the following line for exiting app.
                //e.Handled = true;
                await Launcher.LaunchUriAsync(new Uri("mailto:myhDev@live.com?subject=TAQ%20App異常回報&body=請寄送以下app異常訊息給開發者，謝謝。%0D%0A版本：" + vm.Version + "%0D%0A例外：" + e.Exception.ToString().Replace("\r\n", "%0D%0A") + "%0D%0A若能提供其他造成異常的資訊，可使開發者更快找出問題，謝謝。"));
            };
        }

        private void initLocalSettings()
        {
            localSettings =
       ApplicationData.Current.LocalSettings;
            if (localSettings.Values["AppTheme"] == null)
            {
                localSettings.Values["AppTheme"] = true;
            }
            this.RequestedTheme = (bool)localSettings.Values["AppTheme"] ? ApplicationTheme.Dark : ApplicationTheme.Light;

            if (localSettings.Values["TileClearSty"] == null)
            {
                localSettings.Values["TileClearSty"] = true;
            }
            if (localSettings.Values["MapColor"] == null)
            {
                localSettings.Values["MapColor"] = true;
            }
            if (localSettings.Values["MapAutoPos"] == null)
            {
                localSettings.Values["MapAutoPos"] = true;
            }
            if (localSettings.Values["SecondSitesNotify"] == null)
            {
                localSettings.Values["SecondSitesNotify"] = true;
            }
            if (localSettings.Values["WarnStateChangeMode"] == null)
            {
                localSettings.Values["WarnStateChangeMode"] = true;
            }
            if (localSettings.Values["AQI_Limit"] == null)
            {
                localSettings.Values["AQI_Limit"] = 50.0;
            }
            if (localSettings.Values["PM2.5_Limit"] == null)
            {
                localSettings.Values["PM2.5_Limit"] = 15.4;
            }
            if (localSettings.Values["MainSite"] == null)
            {
                localSettings.Values["MainSite"] = "平鎮";
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        /// 
        public string tappedId;
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            tappedId = e.TileId;
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
