using DynamicData.Binding;
using Newtonsoft.Json;
using SonicExplorerLib;
using SonicExplorerLib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Prism.Commands;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SonicExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private LuceneContentSearch search;
        private volatile string lastKey;
        private bool showRecentFiles = true;
        private List<SearchResult> RecentlyOpenedList;


        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<RecentOpenItem> recentFiles;

        public MainPage()
        {
            this.InitializeComponent();
            this.recentFiles = new ObservableCollection<RecentOpenItem>();
            SearchResultService.instance.refreshSearch += ((sender, args) =>
            {
                search = new LuceneContentSearch();
                _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllowSearch)));
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowWelcome)));
                    });
            });
        }

        public bool AllowSearch => SettingsContainer.instance.Value.GetValue<bool>("indexingComplete") == true;

        public bool ShowWelcome => SettingsContainer.instance.Value.GetValue<bool>("indexingComplete") != true;

        public bool ShowRecentFiles
        {
            get => this.showRecentFiles;
            set
            {
                if (showRecentFiles != value)
                {
                    this.showRecentFiles = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowRecentFiles)));
                }
            }
        }

        public ObservableCollection<SearchResultItem> SearchResults => SearchResultService.instance.SearchResults;

        public ObservableCollection<RecentOpenItem> RecentFiles
        {
            get => this.recentFiles;
            set
            {
                this.recentFiles = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(recentFiles)));
            }
        }

        private void mySearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            search?.SearchForFileOrFolder(args.QueryText.ToLower());
        }

        private void mySearchBox_QueryChanged(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            search?.SearchRealtimeForFileOrFolder(args.QueryText.ToLower());
            ShowRecentFiles = false;
            lastKey = args.QueryText;
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (AllowSearch)
            {
                search = new LuceneContentSearch();
            }

            string RecentlyOpenedJsonString = SettingsContainer.instance.Value.GetValue<string>("recentlyOpened");
            if (RecentlyOpenedJsonString != null)
            {
                RecentlyOpenedList = JsonConvert.DeserializeObject<List<SearchResult>>(RecentlyOpenedJsonString);
                RecentlyOpenedList.ForEach(x => recentFiles.Insert(0, new RecentOpenItem(x)));
            }
        }


        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            SearchResultItem item = e.ClickedItem as SearchResultItem;
            if (item.SearchResult.isFolder)
            {
                await Launcher.LaunchFolderPathAsync(item.SearchResult.path);
            }
            else
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(item.SearchResult.path);
                await Launcher.LaunchFileAsync(file);
                var recent = new SearchResult
                {
                    fileName = file.DisplayName,
                    path = file.Path,
                    isFolder = false
                };
                MRUCacheList.instance.AddItem(recent);
                MRUCacheList.instance.RecentFilesList.ForEach(x =>
                {
                    int idx = AlreadyPresent(x);
                    if (idx >= 0)
                    {
                        this.recentFiles.RemoveAt(idx);
                    }

                    if (this.recentFiles.Count < 4)
                    {
                        this.recentFiles.Insert(0, new RecentOpenItem(x));
                    }
                    else
                    {
                        int lastIdx = this.recentFiles.Count - 1;
                        this.recentFiles.RemoveAt(lastIdx);
                        this.recentFiles.Insert(0, new RecentOpenItem(x));
                    }
                });
            }
        }

        public int AlreadyPresent(SearchResult item)
        {
            int cnt = this.recentFiles.Count;
            for (int i = 0; i < cnt; i++)
            {
                if (this.recentFiles[i].RecentItems.path == item.path)
                {
                    return i;
                }
            }
            return -1;
        }

        private void RadioButton_Checked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (search != null)
            {
                RadioButton rb = sender as RadioButton;
                search?.SelectSearchSegment(rb.Name);
                search = new LuceneContentSearch();
                if (!string.IsNullOrWhiteSpace(lastKey))
                {
                    search?.SearchRealtimeForFileOrFolder(lastKey.ToLower());
                }
            }
        }


        private async void RecentListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            RecentOpenItem item = e.ClickedItem as RecentOpenItem;
            StorageFile file = await StorageFile.GetFileFromPathAsync(item.RecentItems.path);
            await Launcher.LaunchFileAsync(file);
            var recent = new SearchResult
            {
                fileName = file.DisplayName,
                path = file.Path,
                isFolder = false
            };
            MRUCacheList.instance.AddItem(recent);
            MRUCacheList.instance.RecentFilesList.ForEach(x =>
            {
                int idx = AlreadyPresent(x);
                if (idx >= 0)
                {
                    this.recentFiles.RemoveAt(idx);
                }

                if (this.recentFiles.Count < 4)
                {
                    this.recentFiles.Insert(0, new RecentOpenItem(x));
                }
                else
                {
                    int lastIdx = this.recentFiles.Count - 1;
                    this.recentFiles.RemoveAt(lastIdx);
                    this.recentFiles.Insert(0, new RecentOpenItem(x));
                }
            });
        }
    }
}
