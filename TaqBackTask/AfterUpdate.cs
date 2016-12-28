using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace TaqBackTask
{
    public sealed class AfterUpdate : IBackgroundTask
    {
        private ApplicationDataContainer localSettings;
        BackgroundTaskDeferral deferral;

        public AfterUpdate()
        {
            localSettings = ApplicationData.Current.LocalSettings;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Don't place any code (including debug code) before GetDeferral!!!
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            var tbtLog = await ApplicationData.Current.LocalFolder.CreateFileAsync("AfterUpdateLog.txt", CreationCollisionOption.ReplaceExisting);
            var s = await tbtLog.OpenStreamForWriteAsync();
            var sw = new StreamWriter(s);
            sw.WriteLine("Background task start time: " + DateTime.Now.ToString());
            try
            {
                sw.WriteLine("Unregister old background tasks start: " + DateTime.Now.ToString());
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name != "AfterUpdateBackTask")
                    {
                        task.Value.Unregister(true);
                    }
                }
                sw.WriteLine("Unregister old background tasks end: " + DateTime.Now.ToString());

                sw.WriteLine("Register new background tasks start: " + DateTime.Now.ToString());
                /*
                await BackTaskReg.RegisterBackgroundTask("UserPresentBackTask", "TaqBackTask.UserPresentBackTask", new SystemTrigger(SystemTriggerType.UserPresent, false));
                await BackTaskReg.RegisterBackgroundTask("UserAwayBackTask", "TaqBackTask.UserAwayBackTask", new SystemTrigger(SystemTriggerType.UserAway, false));*/
                await BackTaskReg.UserPresentTaskReg(Convert.ToUInt32(localSettings.Values["BgUpdatePeriod"]));
                sw.WriteLine("Register new background tasks end: " + DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
            }
            finally
            {
                sw.WriteLine("Background task end time: " + DateTime.Now.ToString());
                sw.Flush();
                s.Dispose();
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
