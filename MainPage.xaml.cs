using DynamicData.Binding;
using SonicExplorerLib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
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

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
            ContentIndexer.GetInstance.IndexingPercentageObservable.Subscribe(value =>
            {
                this.IndexingBar.Value = value;
            });
            SearchResultService.instance.refreshSearch += ((sender, args) =>
            {
                search = new LuceneContentSearch();
                _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllowSearch)));
                    });
            });
        }

        public bool AllowSearch => SettingsContainer.instance.Value.GetValue<bool>("indexingComplete") == true;

        public ObservableCollection<string> SearchResults => SearchResultService.instance.SearchResults;

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
            search = new LuceneContentSearch();
        }
    }
}
