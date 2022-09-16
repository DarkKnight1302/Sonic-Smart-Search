using SonicExplorerLib.BackgroundTaskImpl;
using Windows.ApplicationModel.Background;

namespace BackgroundTasks
{
    public sealed class TimerTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
            => await TimerTaskImpl.Instance.RunImplAsync(taskInstance).ConfigureAwait(false);
    }
}
