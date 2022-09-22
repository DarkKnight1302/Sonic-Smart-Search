using Lucene.Net.Search;
using System;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Index;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using SonicExplorerLib.Models;

namespace SonicExplorerLib
{
    public class LuceneContentSearch
    {
        private string[] IndexedLocations = { "documents", "downloads", "desktop", "pictures", "music", "videos" };
        private List<IndexSearcher> searchers = new List<IndexSearcher>();

        public LuceneContentSearch()
        {
            // Construct a machine-independent path for the index
            foreach (string location in IndexedLocations)
            {
                try
                {
                    string basePath = Environment.GetFolderPath(
                       Environment.SpecialFolder.CommonApplicationData);
                    string indexPath = Path.Combine(basePath, $"hyperXindex-{location}");
                    var dir = FSDirectory.Open(indexPath);
                    var reader = DirectoryReader.Open(dir);
                    var searcher = new IndexSearcher(reader);
                    searchers.Add(searcher);
                } catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public async void SearchForFileOrFolder(string keyword)
        {
            await SearchResultService.instance.ClearList().ConfigureAwait(false);
            keyword = keyword?.Trim();
            if (string.IsNullOrEmpty(keyword) || keyword.Length < 2 || string.IsNullOrWhiteSpace(keyword))
            {
                return;
            }
            List<Task> searchTasks = new List<Task>();
            CancellationTokenSource source = new CancellationTokenSource();
            foreach (IndexSearcher searcher in searchers)
            {
                searchTasks.Add(Task.Run(() => GetFilePaths(searcher, keyword, source.Token, source, 0)));
            }
        }

        public async void SearchRealtimeForFileOrFolder(string keyword)
        {
            await SearchResultService.instance.ClearList().ConfigureAwait(false);
            keyword = keyword?.Trim();
            if (string.IsNullOrEmpty(keyword) || keyword.Length < 3 || string.IsNullOrWhiteSpace(keyword))
            {
                return;
            }
            List<Task> searchTasks = new List<Task>();
            CancellationTokenSource source = new CancellationTokenSource();
            foreach (IndexSearcher searcher in searchers)
            {
                searchTasks.Add(Task.Run(() => GetFilePaths(searcher, keyword, source.Token, source, 0)));
            }
        }

        private bool GetFilePaths(IndexSearcher searcher, string keyword, CancellationToken cancellationToken, CancellationTokenSource source, int rank)
        {
            TermQuery termQuery = new TermQuery(new Term("name", keyword));
            TopDocs docs = searcher.Search(termQuery, 3);
            List<SearchResult> paths = new List<SearchResult>();
            if (docs.TotalHits == 0 && keyword.Any(x => Char.IsWhiteSpace(x)) && !cancellationToken.IsCancellationRequested)
            {
                string[] splitwords = keyword.Split(' ');
                foreach (string split in splitwords)
                {
                    if (split.Length < 2)
                    {
                        continue;
                    }
                    if (GetFilePaths(searcher, split, cancellationToken, source, rank))
                    {
                        return true;
                    }
                }
            }
            if (docs.TotalHits == 0)
            {
                var wildcardQuery = new WildcardQuery(new Term("name", $"*{keyword}*"));
                docs = searcher.Search(wildcardQuery, 3);
                rank++;
            }
            if (docs.TotalHits == 0 && !cancellationToken.IsCancellationRequested)
            {
                var phrase = new FuzzyQuery(new Term("name", keyword), 2);
                docs = searcher.Search(phrase, 3);
                rank++;
            }
            if (docs.TotalHits == 0 && !cancellationToken.IsCancellationRequested)
            {
                rank++;
                docs = SubstringMatching(searcher, keyword, cancellationToken);
                if (docs == null)
                {
                    return false;
                }
            }
            if (docs.TotalHits == 0)
            {
                Debug.WriteLine($"IS cancellation requested {cancellationToken.IsCancellationRequested}");
                return false;
            }
            source.Cancel();
            foreach (ScoreDoc hit in docs.ScoreDocs)
            {
                var foundDoc = searcher.Doc(hit.Doc);
                paths.Add(new SearchResult
                {
                    fileName = foundDoc.Get("name"),
                    path = foundDoc.Get("path"),
                    isFolder = foundDoc.Get("folder") != null
                });   
            }
            SearchResultService.instance.AddItem(paths, rank);
            return true;
        }

        private TopDocs SubstringMatching(IndexSearcher searcher, string keyword, CancellationToken cancellationToken)
        {
            int size = keyword.Length;
            for(int window = size-1 ; window > 1; window--)
            {
                for(int i=0; (i+window) <= size; i++)
                {
                    var substring = keyword.Substring(i, window);
                    var wildcardQuery = new WildcardQuery(new Term("name", $"*{substring}*"));
                    var docs = searcher.Search(wildcardQuery, 3);
                    if (docs.TotalHits > 0 || cancellationToken.IsCancellationRequested)
                    {
                        return docs;
                    }
                }
            }
            return null;
        }
    }
}
