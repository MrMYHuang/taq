using System;
using Taq;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using System.IO;
using TaqShared;
using Windows.UI.Xaml.Media.Imaging;

namespace TaqBackTask
{
    public sealed class TaqBackTask : XamlRenderingBackgroundTask
    {
        private static TaqModel m = new TaqModel();

        protected async override void OnRun(IBackgroundTaskInstance taskInstance)
        {
#if DEBUG
            var tbtLog = await ApplicationData.Current.LocalFolder.CreateFileAsync("TbtLog.txt", CreationCollisionOption.OpenIfExists);
            using (var s = await tbtLog.OpenStreamForWriteAsync())
            {
                s.Seek(0, SeekOrigin.End);
                var sw = new StreamWriter(s);
                var ct = DateTime.Now;
                sw.WriteLine(ct.ToString());
                sw.Flush();
            }
#endif
            // We assume that this method has a high probability of a successfull run
            // after a failed run with exceptions. It means the success rate of a run is almost independent of the previous runs. So, we just catch exceptions and do nothing, so that this baskgroundtask won't crash and exit.
            try
            {
                // Get a deferral, to prevent the task from closing prematurely
                // while asynchronous code is still running.
                BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

                // Download the feed.
                var res = await m.downloadDataXml();
                await m.loadAqXml();
                m.convertXDoc2Dict();
                await m.loadCurrSite();

                var medTile = new MedTile();
                await m.getMedTile(medTile);
                // Update the live tile with the feed items.
                await m.updateLiveTile();

                // Send notifications.
                m.sendNotifications();

                // Inform the system that the task is finished.
                deferral.Complete();
            }
            catch (Exception ex)
            {
                // Do nothing.
            }
        }

        volatile bool _cancelRequested = false;
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //
            // Indicate that the background task is canceled.
            //
            _cancelRequested = true;
        }
    }
}
