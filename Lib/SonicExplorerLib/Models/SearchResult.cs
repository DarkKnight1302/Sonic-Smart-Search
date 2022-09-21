namespace SonicExplorerLib.Models
{
    public class SearchResult
    {
        public string fileName { get; set; }

        public string path { get; set; }

        public bool isFolder { get; set; }

        public override string ToString()
        {
            return $"{fileName}_{path}_{isFolder}";
        }
    }
}
