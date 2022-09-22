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
        private CoreDispatcher Dispatcher;
        public static MRUCacheList instance => lazyInstance.Value;

        private static Lazy<MRUCacheList> lazyInstance = new Lazy<MRUCacheList>(() => new MRUCacheList());

        public MRUCacheList()
        {
            RecentFilesList = new ObservableCollection<RecentOpenItem>();
            this.Dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
        }

        public async void AddItem(RecentItems recentPath)
        {
            await this.SemaphoreSlim.WaitAsync();
            try
            {
                var RecentFileItem = new RecentOpenItem(recentPath);
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    int idx = AlreadyPresent(recentPath);
                    if (idx >= 0)
                    {
                        RecentFilesList.RemoveAt(idx);
                    }

                    if (RecentFilesList.Count < 4)
                    {
                        RecentFilesList.Insert(0, RecentFileItem);
                    }
                    else
                    {
                        int lastIdx = RecentFilesList.Count - 1;
                        RecentFilesList.RemoveAt(lastIdx);
                        RecentFilesList.Insert(0, RecentFileItem);
                        
                    }
                });
            }
            finally
            {
                this.SemaphoreSlim.Release();
            }            
        }

        public int AlreadyPresent(RecentItems item)
        {
            RecentOpenItem RecentFileItem = new RecentOpenItem(item);
            int cnt = RecentFilesList.Count;
            for (int i = 0; i<cnt; i++)
            {
                if(RecentFilesList[i].RecentItems.path == RecentFileItem.RecentItems.path)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
