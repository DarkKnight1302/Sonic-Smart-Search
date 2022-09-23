using Prism.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SonicExplorerLib.Models
{
    public sealed partial class SearchResultItem : UserControl, IEquatable<SearchResultItem>
    {
        private ICommand _command1;
        private ICommand _command2;

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

        public ICommand LaunchFileOrFolder => _command1 ??= new DelegateCommand(() => Button_Click_Open());

        public ICommand LaunchOpenWith => _command2 ??= new DelegateCommand(() => Button_Click_OpenWith());

        public bool Equals(SearchResultItem other)
        {
            return this.SearchResult.path.Equals(other.SearchResult.path);
        }

        private async void Button_Click_Open()
        {
            if (this.SearchResult.isFolder)
            {
                await Launcher.LaunchFolderPathAsync(this.SearchResult.path);
            } else
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(this.SearchResult.path);
                await Launcher.LaunchFileAsync(file);
                var recent = new RecentItems
                {
                    fileName = file.DisplayName,
                    path = file.Path,
                };
                MRUCacheList.instance.AddItem(recent);
            }
        }

        private async void Button_Click_OpenWith()
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(this.SearchResult.path);
            await Launcher.LaunchFileAsync(file, new LauncherOptions
            {
                DisplayApplicationPicker = true
            });
            var recent = new RecentItems
            {
                fileName = file.DisplayName,
                path = file.Path,
            };
            MRUCacheList.instance.AddItem(recent);
        }
    }
}
