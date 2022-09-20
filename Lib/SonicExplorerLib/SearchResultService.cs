
using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace SonicExplorerLib
{
    public class SearchResultService
    {
        private SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        private CoreDispatcher Dispatcher;

        public EventHandler refreshSearch;

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

        public async void AddItem(List<string> paths)
        {
            await this.SemaphoreSlim.WaitAsync();
            try
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SearchResults.AddRange(paths);
                    });
            } finally
            {
                this.SemaphoreSlim.Release();
            }
        }

        public async void ClearList()
        {
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
