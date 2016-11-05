using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Background;
using TaqShared;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace TaqBackTask
{
    public sealed class TaqBackTask : IBackgroundTask
    {
        private static Shared shared = new Shared();

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // We assume that this method has a high probability of a successfull run
            // after a failed run with exceptions. It means the success rate of a run is almost independent of the previous runs. So, we just catch exceptions and do nothing, so that this baskgroundtask won't crash and exit.
            try
            {
                // Get a deferral, to prevent the task from closing prematurely
                // while asynchronous code is still running.
                BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

                // Download the feed.
                var res = await shared.downloadDataXml();
                await shared.reloadXd();
                await shared.loadCurrSite();

                // Update the live tile with the feed items.
                shared.updateLiveTile();
                shared.sendNotify();

                // Inform the system that the task is finished.
                deferral.Complete();
            }
            catch (Exception ex)
            {
                // Do nothing.
            }
        }
    }
}
