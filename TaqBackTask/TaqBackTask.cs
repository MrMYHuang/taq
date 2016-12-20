#define LOG_STEP
using System;
using Taq;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;

namespace TaqBackTask
{
    public sealed class TaqBackTask : XamlRenderingBackgroundTask
    {
        BackgroundTaskDeferral deferral;

        protected async override void OnRun(IBackgroundTaskInstance taskInstance)
        {
            // Don't place any code (including debug code) before GetDeferral!!!
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            deferral = taskInstance.GetDeferral();

            var tbtLog = await ApplicationData.Current.LocalFolder.CreateFileAsync("TbtLog.txt", CreationCollisionOption.ReplaceExisting);
            var s = await tbtLog.OpenStreamForWriteAsync();
            var sw = new StreamWriter(s);
            var ct = DateTime.Now;
            sw.WriteLine("Background task start time: " + ct.ToString());

            TaqModel m = new TaqModel();

            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            // We assume that this following codes have a high probability of a successfull run
            // after a failed run with exceptions. It means the success rate of a run is almost independent of the previous runs. So, we just catch exceptions and do nothing, so that this baskgroundtask won't crash and exit.
            try
            {
                sw.WriteLine("Download start time: " + ct.ToString());
                // Download the feed.
                var res = await m.downloadDataXml();
            }
            catch (Exception ex)
            {
                sw.WriteLine("Download fail time: " + ct.ToString());
                // Ignore.
            }

            try
            {
                sw.WriteLine("loadAqXml time: " + ct.ToString());
                await m.loadAqXml();
            }
            catch (Exception ex)
            {
                sw.WriteLine("loadAqXml fail time: " + ct.ToString());
                // Ignore.
            }

            sw.WriteLine("Many calls start time: " + ct.ToString());
            try
            {
                m.convertXDoc2Dict();
                if ((bool)m.localSettings.Values["AutoPos"] && (bool)m.localSettings.Values["BgMainSiteAutoPos"])
                {
                    await m.findNearestSite();
                    m.localSettings.Values["MainSite"] = m.nearestSite;
                }
                await m.loadMainSite();
                await m.loadSubscrSiteXml();

                // Update the live tile with the feed items.
                await m.updateLiveTile();

                // Send notifications.
                m.sendSubscrSitesNotifications();

                // Tell Taq foreground app that data has been updated.
                m.localSettings.Values["TaqBackTaskUpdated"] = true;
                sw.WriteLine("Many calls end time: " + ct.ToString());
            }
            catch (Exception ex)
            {
                // Do nothing.
            }
            finally
            {
                sw.WriteLine("Background task end time: " + ct.ToString());
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
