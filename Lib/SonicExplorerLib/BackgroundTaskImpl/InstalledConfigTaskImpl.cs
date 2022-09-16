using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.ApplicationModel.Background;
using Timer = System.Timers.Timer;

namespace SonicExplorerLib.BackgroundTaskImpl
{
    public class InstalledConfigTaskImpl : BackgroundTaskBase
    {
        int count = 0;

        public InstalledConfigTaskImpl()
        {
            Debug.WriteLine("New install task");
        }

        protected override bool IsAsync => true;

        protected override string TaskName => "InstalledConfigTask";

        protected override string EntryPoint => "BackgroundTasks." + this.TaskName;

        protected override bool IsRegisterAllowed()
        {
            return false;
        }

        protected override IBackgroundTrigger MakeTrigger()
        {
            throw new System.NotImplementedException();
        }

        protected override async Task RunAsync(IBackgroundTaskInstance taskInstance, CancellationToken token)
        {
            Debug.WriteLine($"Run called");
            Timer t = new Timer(1000);
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
