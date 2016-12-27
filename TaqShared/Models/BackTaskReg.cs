using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace TaqBackTask
{
    public static class BackTaskReg
    {
        public async static Task<int> UserPresentTaskReg(uint timerPeriod)
        {
            // Update by timer.
            await timerBackUpdateReg(timerPeriod);
            // Update if the Internet is available.
            await backUpdateReg("HasNet", new SystemTrigger(SystemTriggerType.InternetAvailable, false));
            return 0;
        }

        public static void unregisterAllTasks(string preserrved)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if(task.Value.Name != preserrved)
                {
                    task.Value.Unregister(true);
                }
            }
        }

        public async static Task<int> timerBackUpdateReg(uint timerPeriod)
        {
            await backUpdateReg("Timer", new TimeTrigger(timerPeriod, false));
            return 0;
        }

        public async static Task<int> backUpdateReg(string name, IBackgroundTrigger trigger)
        {
            var btr = await BackTaskReg.RegisterBackgroundTask(name + "TaqBackTask", "TaqBackTask.TaqBackTask", trigger);
            //btr.Completed += Btr_Completed;
            return 0;
        }

        /*
        private async static void Btr_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                statusTextBlock.Text = DateTime.Now.ToString("HH:mm:ss tt") + "更新";
                await ReloadXmlAndSitesData();
            });
        }*/

        public static int unregisterBackTask(string taskName)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    task.Value.Unregister(true);
                }
            }
            return 0;
        }

        public static async Task<BackgroundTaskRegistration> RegisterBackgroundTask(string taskName, string taskEntryPoint, IBackgroundTrigger trigger)
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy ||
                backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed)
            {
                unregisterBackTask(taskName);

                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;
                taskBuilder.SetTrigger(trigger);
                //taskBuilder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                try
                {
                    var btr = taskBuilder.Register();
                    return btr;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }
    }
}
