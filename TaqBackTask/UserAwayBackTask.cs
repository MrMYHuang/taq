using System;
using System.IO;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace TaqBackTask
{
    public sealed class UserAwayBackTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Don't place any code (including debug code) before GetDeferral!!!
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            var tbtLog = await ApplicationData.Current.LocalFolder.CreateFileAsync("UserAwayBackTaskLog.txt", CreationCollisionOption.ReplaceExisting);
            var s = await tbtLog.OpenStreamForWriteAsync();
            var sw = new StreamWriter(s);
            sw.WriteLine("Background task start time: " + DateTime.Now.ToString());
            try
            {
                sw.WriteLine("Unregister all background tasks start: " + DateTime.Now.ToString());
                BackTaskReg.unregisterBackTask("TimerTaqBackTask");
                BackTaskReg.unregisterBackTask("HasNetTaqBackTask");
                sw.WriteLine("Unregister all background tasks end: " + DateTime.Now.ToString());
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
