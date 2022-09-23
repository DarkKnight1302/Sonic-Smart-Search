using Newtonsoft.Json;
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
        public List<SearchResult>RecentFilesList { get; private set; }
        public static MRUCacheList instance => lazyInstance.Value;

        private static Lazy<MRUCacheList> lazyInstance = new Lazy<MRUCacheList>(() => new MRUCacheList());

        public MRUCacheList()
        {
            RecentFilesList = new List<SearchResult>();
        }

        public void AddItem(SearchResult recentPath)
        {
            // var RecentFileItem = new SearchResultItem(recentPath);
            int idx = AlreadyPresent(recentPath);
            if (idx >= 0)
            {
                RecentFilesList.RemoveAt(idx);
            }

            if (RecentFilesList.Count < 4)
            {
                RecentFilesList.Add(recentPath);
            }
            else
            {
                int lastIdx = RecentFilesList.Count - 1;
                RecentFilesList.RemoveAt(lastIdx);
                RecentFilesList.Add(recentPath);
            }
            string RecentlyOpenedJsonString = JsonConvert.SerializeObject(RecentFilesList);
            SettingsContainer.instance.Value.SetValue("recentlyOpened", RecentlyOpenedJsonString);
        }            
        

        public int AlreadyPresent(SearchResult item)
        {
            int cnt = RecentFilesList.Count;
            for (int i = 0; i<cnt; i++)
            {
                if(RecentFilesList[i].path == item.path)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
