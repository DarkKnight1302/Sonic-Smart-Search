
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using Windows.ApplicationModel.Background;
using Timer = System.Timers.Timer;

namespace SonicExplorerLib.BackgroundTaskImpl
{
    public class TimerTaskImpl : BackgroundTaskBase
    {
        int count = 0;

        public static TimerTaskImpl Instance = new TimerTaskImpl();

        protected override bool IsAsync => true;

        protected override string TaskName => "TimerTask";

        protected override string EntryPoint => "BackgroundTasks." + this.TaskName;

        public int GetCount => this.count;

        protected override bool IsRegisterAllowed()
        {
            return true;
        }

        protected override IEnumerable<IBackgroundCondition> TriggerConditions => new IBackgroundCondition[]
       {
            new SystemCondition(SystemConditionType.UserPresent),
       };

        protected override IBackgroundTrigger MakeTrigger() => new TimeTrigger(150, false);

        protected override async Task RunAsync(IBackgroundTaskInstance taskInstance, CancellationToken token)
        {
            await ContentIndexer.GetInstance.IndexDataInBackground();
        }
    }
}
