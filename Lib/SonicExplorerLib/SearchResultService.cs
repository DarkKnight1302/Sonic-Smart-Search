
using DynamicData;
using Lucene.Net.Analysis.Payloads;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace SonicExplorerLib
{
    public class SearchResultService
    {
        private SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        private CoreDispatcher Dispatcher;

        public EventHandler refreshSearch;

        private volatile int topRank = int.MaxValue;

        public ObservableCollection<string> SearchResults { get; private set; }

        public static SearchResultService instance => lazyInstance.Value;

        private static Lazy<SearchResultService> lazyInstance = new Lazy<SearchResultService>(() => new SearchResultService());

        private SearchResultService()
        {
            SearchResults = new ObservableCollection<string>();
            this.Dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
        }

        public void RefreshSearch()
        {
            this.refreshSearch?.Invoke(this, null);
        }

        public async void AddItem(List<string> paths, int rank)
        {
            if (rank > topRank)
            {
                return;
            }
            topRank = rank;
            await this.SemaphoreSlim.WaitAsync();
            try
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (SearchResults.Count < 10)
                        {
                            SearchResults.AddRange(paths);
                        }
                    });
            } finally
            {
                this.SemaphoreSlim.Release();
            }
        }

        public async Task ClearList()
        {
            topRank = int.MaxValue;
            await this.SemaphoreSlim.WaitAsync();
            try
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    SearchResults.Clear();
                });
            }
            finally
            {
                this.SemaphoreSlim.Release();
            }
        }
    }
}
