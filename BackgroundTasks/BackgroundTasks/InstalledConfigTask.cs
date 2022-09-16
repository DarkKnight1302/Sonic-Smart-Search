using SonicExplorerLib.BackgroundTaskImpl;
using Windows.ApplicationModel.Background;

namespace BackgroundTasks
{
    public sealed class InstalledConfigTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
            => await new InstalledConfigTaskImpl().RunImplAsync(taskInstance).ConfigureAwait(false);
    }
}
