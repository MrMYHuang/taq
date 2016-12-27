﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Taq;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace TaqBackTask
{
    public sealed class UserPresentBackTask : IBackgroundTask
    {
        private ApplicationDataContainer localSettings;
        public UserPresentBackTask()
        {
            localSettings = ApplicationData.Current.LocalSettings;
        }

        BackgroundTaskDeferral deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Don't place any code (including debug code) before GetDeferral!!!
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            try
            {
                var tbtLog = await ApplicationData.Current.LocalFolder.CreateFileAsync("UserPresentBackTaskLog.txt", CreationCollisionOption.ReplaceExisting);
                var s = await tbtLog.OpenStreamForWriteAsync();
                var sw = new StreamWriter(s);
                sw.WriteLine("Background task start time: " + DateTime.Now.ToString()); TaqModel m = new TaqModel();

                sw.WriteLine("User present tasks reg start: " + DateTime.Now.ToString());
                await BackTaskReg.UserPresentTaskReg(Convert.ToUInt32(localSettings.Values["BgUpdatePeriod"]));

                taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
                sw.WriteLine("Background task end time: " + DateTime.Now.ToString());
                sw.Flush();
                s.Dispose();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                // Inform the system that the task is finished.
                deferral.Complete();
            }
        }

        volatile bool _cancelRequested = false;
        private async void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
#if LOG_STEP
            var tbtLogC = await ApplicationData.Current.LocalFolder.CreateFileAsync("TbtCancelled.txt", CreationCollisionOption.ReplaceExisting);
            using (var s = await tbtLogC.OpenStreamForWriteAsync())
            {
                var sw = new StreamWriter(s);
                var ct = DateTime.Now;
                sw.WriteLine("Background task cancellation time: " + ct.ToString());
                sw.Flush();
            }
#endif
            //
            // Indicate that the background task is canceled.
            //
            _cancelRequested = true;
            deferral.Complete();
        }
    }
}
