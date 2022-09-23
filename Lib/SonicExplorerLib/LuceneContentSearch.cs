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
        private static IDictionary<string, bool> LocationsDictionary = new Dictionary<string, bool>
        {
            {"documents", true},
            {"downloads", true},
            {"desktop", true},
            {"pictures", true},
            {"music", true},
            {"videos", true},
        };

        private List<IndexSearcher> searchers = new List<IndexSearcher>();

        public LuceneContentSearch()
        {
            // Construct a machine-independent path for the index
            foreach (KeyValuePair<string, bool> location in LocationsDictionary)
            {
                try
                {
                    if (!location.Value)
                    {
                        continue;
                    }
                    string basePath = Environment.GetFolderPath(
                       Environment.SpecialFolder.CommonApplicationData);
                    string indexPath = Path.Combine(basePath, $"hyperXindex-{location.Key}");
                    var dir = FSDirectory.Open(indexPath);
                    var reader = DirectoryReader.Open(dir);
                    var searcher = new IndexSearcher(reader);
                    searchers.Add(searcher);
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void SelectSearchSegment(string segment)
        {
            switch (segment)
            {
                case "all":
                    foreach (string key in LocationsDictionary.Keys.ToList())
                    {
                        if (LocationsDictionary[key] == false)
                        {
                            LocationsDictionary[key] = true;
                        }
                    }
                    break;
                default:
                    foreach (string key in LocationsDictionary.Keys.ToList())
                    {
                        LocationsDictionary[key] = false;
                    }
                    LocationsDictionary[segment] = true;
                    break;
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
                searchTasks.Add(Task.Run(() => GetFilePaths(searcher, keyword, source.Token, source, 0, false)));
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
                searchTasks.Add(Task.Run(() => GetFilePaths(searcher, keyword, source.Token, source, 0, false)));
            }
        }

        private bool GetFilePaths(IndexSearcher searcher, string keyword, CancellationToken cancellationToken, CancellationTokenSource source, int rank, bool split)
        {
            bool resultFound = false;
            // Reduce weight for very short keywords while splitting.
            if (keyword.Length < 3)
            {
                rank++;
            }

            TermQuery termQuery = new TermQuery(new Term("name", keyword));
            TopDocs docs = searcher.Search(termQuery, 10);

            if (docs.TotalHits > 0)
            {
                Task.Run(() => PushToResult(docs, rank, searcher));
                resultFound = true;
            }

            if (keyword.Any(x => Char.IsWhiteSpace(x)) && !cancellationToken.IsCancellationRequested)
            {
                string[] splitwords = keyword.Split(' ');
                foreach (string splitKeys in splitwords)
                {
                    if (splitKeys.Length < 2 || string.IsNullOrWhiteSpace(splitKeys))
                    {
                        continue;
                    }
                    Task.Run(() => GetFilePaths(searcher, splitKeys, cancellationToken, source, rank, true));
                }
                return true;
            }

            var wildcardQuery = new WildcardQuery(new Term("name", $"*{keyword}*"));
            docs = searcher.Search(wildcardQuery, 10);

            if (docs.TotalHits > 0)
            {
                resultFound = true;
                Task.Run(() => PushToResult(docs, rank, searcher));
                return true;
            }
            if (!resultFound && !cancellationToken.IsCancellationRequested)
            {
                var phrase = new FuzzyQuery(new Term("name", keyword), 1);
                docs = searcher.Search(phrase, 3);
                if (docs.TotalHits > 0)
                {
                    resultFound = true;
                }
                rank++;
            }
            if (!resultFound && !cancellationToken.IsCancellationRequested)
            {
                var phrase = new FuzzyQuery(new Term("name", keyword), 2);
                docs = searcher.Search(phrase, 3);
                if (docs.TotalHits > 0)
                {
                    resultFound = true;
                }
                rank++;
            }
            if (!resultFound && !cancellationToken.IsCancellationRequested)
            {
                rank++;
                docs = SubstringMatching(searcher, keyword, cancellationToken);
                if (docs == null)
                {
                    return false;
                }
                if (docs.TotalHits > 0)
                {
                    resultFound = true;
                }
            }
            if (!resultFound)
            {
                Debug.WriteLine($"IS cancellation requested {cancellationToken.IsCancellationRequested}");
                return false;
            }
            if (!split)
            {
                source.Cancel();
            }
            PushToResult(docs, rank, searcher);
            return true;
        }

        private void PushToResult(TopDocs docs, int rank, IndexSearcher searcher)
        {
            List<SearchResult> paths = new List<SearchResult>();
            foreach (ScoreDoc hit in docs.ScoreDocs)
            {
                var foundDoc = searcher.Doc(hit.Doc);
                paths.Add(new SearchResult
                {
                    fileName = foundDoc.Get("displayName"),
                    path = foundDoc.Get("path"),
                    isFolder = foundDoc.Get("folder") != null
                });   
            }
            SearchResultService.instance.AddItem(paths, rank);
        }

        private TopDocs SubstringMatching(IndexSearcher searcher, string keyword, CancellationToken cancellationToken)
        {
            int size = keyword.Length;
            for (int window = size - 1; window > 1; window--)
            {
                for (int i = 0; (i + window) <= size; i++)
                {
                    var substring = keyword.Substring(i, window);
                    var wildcardQuery = new WildcardQuery(new Term("name", $"*{substring}*"));
                    var docs = searcher.Search(wildcardQuery, 3);
                    if (docs.TotalHits > 0 || cancellationToken.IsCancellationRequested)
                    {
                        return docs;
                    }
                    var phrase = new FuzzyQuery(new Term("name", substring), 2);
                    docs = searcher.Search(phrase, 3);
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
