using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using System;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Index;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SonicExplorerLib
{
    public class LuceneContentSearch
    {
        private string[] IndexedLocations = { "documents", "downloads" };
        private List<IndexSearcher> searchers = new List<IndexSearcher>();
 
        public LuceneContentSearch()
        {
            // Construct a machine-independent path for the index
            foreach (string location in IndexedLocations)
            {
                string basePath = Environment.GetFolderPath(
                   Environment.SpecialFolder.CommonApplicationData);
                string indexPath = Path.Combine(basePath, $"hyperXindex-{location}");
                var dir = FSDirectory.Open(indexPath);
                var reader = DirectoryReader.Open(dir);
                var searcher = new IndexSearcher(reader);
                searchers.Add(searcher);
            }
        }

        public async void SearchForFileOrFolder(string keyword)
        {
            List<Task> searchTasks = new List<Task>();
            foreach (IndexSearcher searcher in searchers)
            {
                searchTasks.Add(Task.Run(() => GetFilePaths(searcher, keyword)));
            }
            await Task.WhenAll(searchTasks).ConfigureAwait(false);
        }

        private void GetFilePaths(IndexSearcher searcher, string keyword)
        {
            TermQuery termQuery = new TermQuery(new Term("name", keyword));
            TopDocs docs = searcher.Search(termQuery, 3);
            List<string> paths = new List<string>();
            if (docs.TotalHits == 0)
            {
                var wildcardQuery = new WildcardQuery(new Term("name", $"*{keyword}*"));
                docs = searcher.Search(wildcardQuery, 3);
            }
            if (docs.TotalHits == 0)
            {
                var phrase = new FuzzyQuery(new Term("name", keyword), 1);
                docs = searcher.Search(phrase, 3);
            }
            if (docs.TotalHits == 0)
            {
                return;
            }
            foreach (ScoreDoc hit in docs.ScoreDocs)
            {
                var foundDoc = searcher.Doc(hit.Doc);   
                paths.Add(foundDoc.Get("path"));
                Debug.WriteLine($"Path found {foundDoc.Get("path")}");
            }
        }
    }
}
