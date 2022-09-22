using SonicExplorerLib;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SonicExplorer
{
    public sealed partial class Welcome : Page
    {
        public Welcome()
        {
            this.InitializeComponent();
            ContentIndexer.GetInstance.IndexingPercentageObservable.Subscribe(value =>
            {
                this.IndexProgress.Value = value;
            });
            SearchResultService.instance.refreshSearch += (async (sender, args) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        this.Frame.Navigate(typeof(MainPage), new EntranceNavigationTransitionInfo());
                    });
            });
        }
    }
}
