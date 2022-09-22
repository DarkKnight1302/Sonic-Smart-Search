using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SonicExplorerLib.Models
{
    public sealed partial class SearchResultItem : UserControl
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
    }
}
