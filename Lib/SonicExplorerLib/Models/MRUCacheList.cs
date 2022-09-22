using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace SonicExplorerLib.Models
{
    public class MRUCacheList
    {
        private SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        public ObservableCollection<RecentOpenItem>RecentFilesList { get; private set; }
        private volatile int maxCacheSize = 4;
        private CoreDispatcher Dispatcher;
        public static MRUCacheList instance => lazyInstance.Value;

        private static Lazy<MRUCacheList> lazyInstance = new Lazy<MRUCacheList>(() => new MRUCacheList());

        public MRUCacheList()
        {
            RecentFilesList = new ObservableCollection<RecentOpenItem>();
            this.Dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
        }

        public async void AddItem(List<RecentItems> recentPaths)
        {
            await this.SemaphoreSlim.WaitAsync();
            try
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (recentPaths.Count < maxCacheSize)
                    {
                        recentPaths.ForEach(x => RecentFilesList.Add(new RecentOpenItem(x)));
                    }
                    else
                    {
                        RecentFilesList.RemoveAt(0);
                        recentPaths.ForEach(x => RecentFilesList.Add(new RecentOpenItem(x)));
                    }
                });
            }
            finally
            {
                this.SemaphoreSlim.Release();
            }            
        }
    }
}
