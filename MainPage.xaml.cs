using DynamicData.Binding;
using SonicExplorer.Views;
using SonicExplorerLib;
using SonicExplorerLib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SonicExplorer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private LuceneContentSearch search;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<RecentItems> recentPaths;

        public MainPage()
        {
            this.InitializeComponent();
             recentPaths = new List<RecentItems>();
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

        public ObservableCollection<SearchResultItem> SearchResults => SearchResultService.instance.SearchResults;
        public ObservableCollection<RecentOpenItem> RecentFiles => MRUCacheList.instance.RecentFilesList;
        
        private void mySearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            search?.SearchForFileOrFolder(args.QueryText.ToLower());
        }

        private void mySearchBox_QueryChanged(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            search?.SearchRealtimeForFileOrFolder(args.QueryText.ToLower());
        }

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (AllowSearch)
            {
                search = new LuceneContentSearch();
            }
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            string path = e.ClickedItem as string;
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            await Launcher.LaunchFileAsync(file);
            recentPaths.Add(new RecentItems
            {
                fileName = file.DisplayName,
                path = file.Path,
            });
            MRUCacheList.instance.AddItem(recentPaths);
        }
    }
}
