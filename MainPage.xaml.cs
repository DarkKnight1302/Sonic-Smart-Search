﻿using DynamicData.Binding;
using SonicExplorerLib;
using SonicExplorerLib.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
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
        
        private void mySearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            search?.SearchForFileOrFolder(args.QueryText.ToLower());
        }

        private void mySearchBox_QueryChanged(SearchBox sender, SearchBoxQueryChangedEventArgs args)
        {
            search?.SearchRealtimeForFileOrFolder(args.QueryText.ToLower());
            lastKey = args.QueryText;
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
            SearchResultItem item = e.ClickedItem as SearchResultItem;
            if (item.SearchResult.isFolder)
            {
                await Launcher.LaunchFolderPathAsync(item.SearchResult.path);
            }
            else
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(item.SearchResult.path);
                await Launcher.LaunchFileAsync(file);
            }
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
    }
}
