using Prism.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SonicExplorerLib.Models
{
    public sealed partial class SearchResultItem : UserControl, IEquatable<SearchResultItem>
    {
        public SearchResultItem(SearchResult searchResult)
        {
            this.InitializeComponent();
            this.SearchResult = searchResult;
            ResultFontIcon.FontFamily = new FontFamily("Segoe MDL2 Assets");
            if (searchResult.isFolder)
            {
                ResultFontIcon.Glyph = "\xED25";
            }
            else
            {
                ResultFontIcon.Glyph = "\xF000";
            }
        }

        public SearchResult SearchResult { get; private set; }

        public string Glyph { get; private set; }

        public bool ShowOpenWith => !this.SearchResult.isFolder;
        public bool ShowOpenInExplorer => this.SearchResult.isFolder;

        public bool Equals(SearchResultItem other)
        {
            return this.SearchResult.path.Equals(other.SearchResult.path);
        }

        private async Task Button_Click_Open(bool openInExplorer)
        {
            if (this.SearchResult.isFolder)
            {
                await Launcher.LaunchFolderPathAsync(this.SearchResult.path);
            } else
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(this.SearchResult.path);
                if (openInExplorer || ((int)file.Attributes) > 300)
                {
                    var parent = await file.GetParentAsync();
                    var folderOptions = new FolderLauncherOptions();
                    folderOptions.ItemsToSelect.Clear();
                    folderOptions.ItemsToSelect.Add(file);
                    await Launcher.LaunchFolderPathAsync(parent.Path, folderOptions);
                    return;
                }
                await Launcher.LaunchFileAsync(file);
                var recent = new SearchResult
                {
                    fileName = file.DisplayName,
                    path = file.Path,
                    isFolder = false
                };
                MRUCacheList.instance.AddItem(recent);
            }
        }

        private async Task Button_Click_OpenWith()
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(this.SearchResult.path);
            if (file != null && ((int)file.Attributes) > 300)
            {
                var parent = await file.GetParentAsync();
                var folderOptions = new FolderLauncherOptions();
                folderOptions.ItemsToSelect.Clear();
                folderOptions.ItemsToSelect.Add(file);
                await Launcher.LaunchFolderPathAsync(parent.Path, folderOptions);
                return;
            }
            await Launcher.LaunchFileAsync(file, new LauncherOptions
            {
                DisplayApplicationPicker = true
            });
            var recent = new SearchResult
            {
                fileName = file.DisplayName,
                path = file.Path,
                isFolder = false
            };
            MRUCacheList.instance.AddItem(recent);
        }

        private async void Button1_Click(object sender, RoutedEventArgs e)
        {
            await Button_Click_OpenWith();
        }

        private async void Button2_Click(object sender, RoutedEventArgs e)
        {
            await Button_Click_Open(false);
        }

        private async void Button3_Click(object sender, RoutedEventArgs e)
        {
            await Button_Click_Open(true);
        }
    }
}
