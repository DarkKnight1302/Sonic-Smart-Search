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
using System.Collections.Concurrent;
using Lucene.Net.Documents;


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
            if (keyword.Length == 1)
            {
                return false;
            }
            if (keyword.Length < 3)
            {
                rank+= 20;
            }
            bool resultFound = false;
            TermQuery termQuery = new TermQuery(new Term("name", keyword));
            TopDocs docs = searcher.Search(termQuery, 10);

            if (docs.TotalHits > 0)
            {
                Task.Run(() => PushToResult(docs, rank, searcher));
                resultFound = true;
                if (!split)
                {
                    source.Cancel();
                }
            }

            var wildcardQuery = new WildcardQuery(new Term("name", $"*{keyword}*"));
            docs = searcher.Search(wildcardQuery, 10);

            if (docs.TotalHits > 0)
            {
                resultFound = true;
                Task.Run(() => PushToResult(docs, rank, searcher));
                if (!split)
                {
                    source.Cancel();
                }
                return true;
            }

            if (keyword.Any(x => Char.IsWhiteSpace(x)) && !cancellationToken.IsCancellationRequested)
            {
                string wildCardKeyWord = keyword.Replace(' ', '*');
                var wildcardSpaceQuery = new WildcardQuery(new Term("name", $"*{wildCardKeyWord}*"));
                docs = searcher.Search(wildcardSpaceQuery, 10);

                if (docs.TotalHits > 0)
                {
                    resultFound = true;
                    Task.Run(() => PushToResult(docs, rank, searcher));
                    source.Cancel();
                    return true;
                }
                string[] splitwords = keyword.Split(' ');
                List<string> splitStrings = new List<string>();
                foreach (string splitKeys in splitwords)
                {
                    if (splitKeys.Length < 3 || string.IsNullOrWhiteSpace(splitKeys))
                    {
                        continue;
                    }
                    splitStrings.Add(splitKeys);
                }
                if (splitStrings.Count > 1)
                {
                    SplitSearch(searcher, splitStrings, cancellationToken, source);
                }
                else
                {
                    foreach (string splitK in splitwords)
                    {
                        Task.Run(() => GetFilePaths(searcher, splitK.Trim(), cancellationToken, source, rank + 1, true));
                    }
                }
                return true;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return true;
            }

            if (!resultFound && !cancellationToken.IsCancellationRequested)
            {
                rank++;
                var phrase = new FuzzyQuery(new Term("name", keyword), 1);
                docs = searcher.Search(phrase, 3);
                if (docs.TotalHits > 0)
                {
                    if (!split)
                    {
                        source.Cancel();
                    }
                    resultFound = true;
                }
            }
            if (!resultFound && !cancellationToken.IsCancellationRequested)
            {
                var phrase = new FuzzyQuery(new Term("name", keyword), 2);
                docs = searcher.Search(phrase, 3);
                if (docs.TotalHits > 0)
                {
                    if (!split)
                    {
                        source.Cancel();
                    }
                    resultFound = true;
                }
                rank++;
            }
            if (!resultFound && !cancellationToken.IsCancellationRequested)
            {
                rank++;
                docs = InternalWildCardMatching(searcher, keyword, cancellationToken);
                if (docs != null && docs.TotalHits > 0)
                {
                    if (!split)
                    {
                        source.Cancel();
                    }
                    resultFound = true;
                }
            }
            if (!resultFound && !cancellationToken.IsCancellationRequested)
            {
                rank++;
                Tuple<TopDocs, int> docTuple = SubstringMatching(searcher, keyword, cancellationToken);
                if (docTuple == null)
                {
                    return false;
                }
                if (docTuple.Item1.TotalHits > 0)
                {
                    resultFound = true;
                    docs = docTuple.Item1;
                    rank += (20 - docTuple.Item2);
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

        private async void SplitSearch(IndexSearcher searcher, List<string> splitKeys, CancellationToken cancellationToken, CancellationTokenSource source)
        {
            ConcurrentDictionary<string, int> fileScore = new ConcurrentDictionary<string, int>();
            ConcurrentDictionary<string, Document> fileDocument = new ConcurrentDictionary<string, Document>();
            List<Task> tasks = new List<Task>();
            foreach (string key in splitKeys)
            {
                tasks.Add(Task.Run(() =>
                {
                    Tuple<TopDocs, int> tuple = SplitKeySearch(searcher, key, cancellationToken);
                    if (tuple != null)
                    {
                        int score = tuple.Item2;
                        foreach (ScoreDoc hit in tuple.Item1.ScoreDocs)
                        {
                            var foundDoc = searcher.Doc(hit.Doc);
                            string fileName = foundDoc.Get("displayName");
                            fileScore.AddOrUpdate(fileName, score, (x, v1) => (v1 + score));
                            fileDocument.AddOrUpdate(fileName, foundDoc, (name, doc) => foundDoc);
                        }
                    }
                }));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
            var sortedDict = from entry in fileScore orderby entry.Value descending select entry;
            int thresholdScore = (splitKeys.Count) * 100;
            int resultCount = 0;
            foreach (var keyValue in sortedDict)
            {
                if (resultCount >= 3)
                {
                    break;
                }
                if (keyValue.Value > thresholdScore)
                {
                    source.Cancel();
                }
                if (fileDocument.TryGetValue(keyValue.Key, out Document scoreDoc))
                {
                    resultCount++;
                    PushToResult(scoreDoc, 1000 - keyValue.Value);
                }
            }
        }

        private Tuple<TopDocs, int> SplitKeySearch(IndexSearcher searcher, string keyword, CancellationToken cancellationToken)
        {
            TermQuery termQuery = new TermQuery(new Term("name", keyword));
            TopDocs docs = searcher.Search(termQuery, 20);
            if (docs.TotalHits > 0)
            {
                return new Tuple<TopDocs, int>(docs, 100);
            }

            var wildcardQuery = new WildcardQuery(new Term("name", $"*{keyword}*"));
            docs = searcher.Search(wildcardQuery, 20);
            if (docs.TotalHits > 0)
            {
                return new Tuple<TopDocs, int>(docs, 100);
            }

            var phrase = new FuzzyQuery(new Term("name", keyword), 1);
            docs = searcher.Search(phrase, 5);
            if (docs.TotalHits > 0)
            {
                return new Tuple<TopDocs, int>(docs, 30);
            }

            phrase = new FuzzyQuery(new Term("name", keyword), 2);
            docs = searcher.Search(phrase, 5);
            if (docs.TotalHits > 0)
            {
                return new Tuple<TopDocs, int>(docs, 20);
            }

            docs = InternalWildCardMatching(searcher, keyword, cancellationToken);
            if (docs != null && docs.TotalHits > 0)
            {
                return new Tuple<TopDocs, int>(docs, 15);
            }

            Tuple<TopDocs, int> topDocTuple = SubstringMatching(searcher, keyword, cancellationToken);
            if (topDocTuple != null && topDocTuple.Item1.TotalHits > 0)
            {
                return new Tuple<TopDocs, int>(topDocTuple.Item1, topDocTuple.Item2);
            }
            return null;
        }

        private void PushToResult(TopDocs docs, int rank, IndexSearcher searcher)
        {
            if (docs == null)
            {
                return;
            }
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

        private void PushToResult(Document foundDoc, int rank)
        {
            var result = new SearchResult
            {
                fileName = foundDoc.Get("displayName"),
                path = foundDoc.Get("path"),
                isFolder = foundDoc.Get("folder") != null
            };
            SearchResultService.instance.AddItem(result, rank);
        }

        private TopDocs InternalWildCardMatching(IndexSearcher searcher, string keyword, CancellationToken cancellationToken)
        {
            for (int i = 1; i < keyword.Length; i++)
            {
                string injectWildCard = $"{keyword.Substring(0, i)}*{keyword.Substring(i, (keyword.Length - i))}";
                var wildcardQuery = new WildcardQuery(new Term("name", $"*{injectWildCard}*"));
                var docs = searcher.Search(wildcardQuery, 3);
                if (docs.TotalHits > 0 || cancellationToken.IsCancellationRequested)
                {
                    return docs;
                }
            }
            return null;
        }

        private Tuple<TopDocs, int> SubstringMatching(IndexSearcher searcher, string keyword, CancellationToken cancellationToken)
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
                        return new Tuple<TopDocs, int>(docs, window);
                    }
                    var phrase = new FuzzyQuery(new Term("name", substring), 2);
                    docs = searcher.Search(phrase, 3);
                    if (docs.TotalHits > 0 || cancellationToken.IsCancellationRequested)
                    {
                        return new Tuple<TopDocs, int>(docs, window);
                    }
                }
            }
            return null;
        }
    }
}
