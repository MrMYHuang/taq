using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace Taq.Shared.Models
{
    public static class BackTaskReg
    {
        public async static Task<int> UserPresentTaskReg(uint timerPeriod)
        {
            // Update by timer.
            await backUpdateReg("Timer", new TimeTrigger(timerPeriod, false));
            // Update if the Internet is available.
            //await backUpdateReg("HasNet", new SystemTrigger(SystemTriggerType.InternetAvailable, false));
            return 0;
        }

        public async static Task<BackgroundTaskRegistration> backUpdateReg(string name, IBackgroundTrigger trigger)
        {
            var btr = await BackTaskReg.RegisterBackgroundTask(name + "BackTask", "Taq.BackTask.TaqBackTask", trigger);
            return btr;
        }

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
                taskBuilder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                try
                {
                    var btr = taskBuilder.Register();
                    return btr;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return null;
                }
            }
            return null;
        }
    }
}
