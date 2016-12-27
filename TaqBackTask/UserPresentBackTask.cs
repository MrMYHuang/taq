using System;
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
    public sealed class UserPresentBackTask : XamlRenderingBackgroundTask
    {
        private ApplicationDataContainer localSettings;
        public UserPresentBackTask()
        {
            localSettings = ApplicationData.Current.LocalSettings;
        }

        BackgroundTaskDeferral deferral;

        protected async override void OnRun(IBackgroundTaskInstance taskInstance)
        {
            // Don't place any code (including debug code) before GetDeferral!!!
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            var tbtLog = await ApplicationData.Current.LocalFolder.CreateFileAsync("UserPresentBackTaskLog.txt", CreationCollisionOption.ReplaceExisting);
            var s = await tbtLog.OpenStreamForWriteAsync();
            var sw = new StreamWriter(s);
            sw.WriteLine("Background task start time: " + DateTime.Now.ToString()); TaqModel m = new TaqModel();

            sw.WriteLine("User present tasks reg start: " + DateTime.Now.ToString());
            await BackTaskReg.UserPresentTaskReg(Convert.ToUInt32(localSettings.Values["BgUpdatePeriod"]));
            sw.WriteLine("User present tasks reg end: " + DateTime.Now.ToString());

            // We assume that this following codes have a high probability of a successfull run
            // after a failed run with exceptions. It means the success rate of a run is almost independent of the previous runs. So, we just catch exceptions and do nothing, so that this baskgroundtask won't crash and exit.
            try
            {
                sw.WriteLine("Download start time: " + DateTime.Now.ToString());
                // Download the feed.
                var res = await m.downloadDataXml();
            }
            catch (Exception ex)
            {
                sw.WriteLine("Download fail time: " + DateTime.Now.ToString());
                // Ignore.
            }

            try
            {
                sw.WriteLine("loadAqXml time: " + DateTime.Now.ToString());
                await m.loadAqXml();
            }
            catch (Exception ex)
            {
                sw.WriteLine("loadAqXml fail time: " + DateTime.Now.ToString());
                // Ignore.
            }

            sw.WriteLine("Many calls start time: " + DateTime.Now.ToString());
            try
            {
                m.convertXDoc2Dict();
                if ((bool)m.localSettings.Values["AutoPos"] && (bool)m.localSettings.Values["BgMainSiteAutoPos"])
                {
                    await m.findNearestSite();
                    await m.loadMainSite(m.nearestSite);
                }
                else
                {
                    await m.loadMainSite((string)m.localSettings.Values["MainSite"]);
                }
                await m.loadSubscrSiteXml();

                // Update the live tile with the feed items.
                await m.updateLiveTile();

                // Send notifications.
                m.sendSubscrSitesNotifications();

                // Tell Taq foreground app that data has been updated.
                m.localSettings.Values["TaqBackTaskUpdated"] = true;
                sw.WriteLine("Many calls end time: " + DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                // Do nothing.
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
