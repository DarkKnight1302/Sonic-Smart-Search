
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
            new SystemCondition(SystemConditionType.InternetAvailable),
            new SystemCondition(SystemConditionType.UserPresent),
       };

        protected override IBackgroundTrigger MakeTrigger() => new TimeTrigger(15, false);

        protected override async Task RunAsync(IBackgroundTaskInstance taskInstance, CancellationToken token)
        {
            Debug.WriteLine($"Run called");
            System.Timers.Timer t = new Timer(1000);
            t.Elapsed += T_Elapsed;
            t.Enabled = true;
            t.Start();
            await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
        }

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            count++;
            Debug.WriteLine($"Timer count value : {count}");
        }
    }
}
