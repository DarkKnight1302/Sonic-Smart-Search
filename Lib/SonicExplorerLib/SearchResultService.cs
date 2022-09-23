
using DynamicData;
using Lucene.Net.Analysis.Payloads;
using SonicExplorerLib.Models;
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

        public ObservableCollection<SearchResultItem> SearchResults { get; private set; }

        public static SearchResultService instance => lazyInstance.Value;

        private static Lazy<SearchResultService> lazyInstance = new Lazy<SearchResultService>(() => new SearchResultService());

        private SearchResultService()
        {
            SearchResults = new ObservableCollection<SearchResultItem>();
            this.Dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
        }

        public void RefreshSearch()
        {
            this.refreshSearch?.Invoke(this, null);
        }

        public async void AddItem(List<SearchResult> paths, int rank)
        {
            if (rank > topRank)
            {
                return;
            }
            await this.SemaphoreSlim.WaitAsync();
            try
            {
                if (rank > topRank)
                {
                    return;
                }
                bool removeItems = false;
                if (rank < topRank)
                {
                    removeItems = true;
                }
                topRank = rank;
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (removeItems)
                        {
                            SearchResults.Clear();
                        }
                        if (SearchResults.Count < 20)
                        {
                            paths.ForEach(x =>
                            {
                                var result = new SearchResultItem(x);
                                if (!SearchResults.Contains(result))
                                {
                                    SearchResults.Add(result);
                                }
                            });
                        }
                    });
            }
            finally
            {
                this.SemaphoreSlim.Release();
            }
        }

        public async void AddItem(SearchResult path, int rank)
        {
            if (rank > topRank)
            {
                return;
            }
            await this.SemaphoreSlim.WaitAsync();
            try
            {
                if (rank > topRank)
                {
                    return;
                }
                bool removeItems = false;
                if (rank < topRank)
                {
                    removeItems = true;
                }
                topRank = rank;
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (removeItems)
                    {
                        SearchResults.Clear();
                    }
                    if (SearchResults.Count < 15)
                    {
                        var result = new SearchResultItem(path);
                        if (!SearchResults.Contains(result))
                        {
                            SearchResults.Add(result);
                        }
                    }
                });
            }
            finally
            {
                this.SemaphoreSlim.Release();
            }
        }

        public async Task ClearList()
        {
            await this.SemaphoreSlim.WaitAsync();
            topRank = int.MaxValue;
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
