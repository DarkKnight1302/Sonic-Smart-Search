using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using System;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Index;
using System.Collections.Generic;
using System.Diagnostics;

namespace SonicExplorerLib
{
    public class LuceneContentSearch
    {
        private StandardAnalyzer analyzer;
        private IndexSearcher searcher;

        public LuceneContentSearch()
        {
            // Construct a machine-independent path for the index
            string basePath = Environment.GetFolderPath(
               Environment.SpecialFolder.CommonApplicationData);
            string indexPath = Path.Combine(basePath, "hyperXindex");
            analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            var dir = FSDirectory.Open(indexPath);
            var reader = DirectoryReader.Open(dir);
            searcher = new IndexSearcher(reader);
        }

        public List<string> GetFilePaths(string keyword)
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
                return null;
            }
            foreach (ScoreDoc hit in docs.ScoreDocs)
            {
                var foundDoc = searcher.Doc(hit.Doc);   
                paths.Add(foundDoc.Get("path"));
                Debug.WriteLine($"Path found {foundDoc.Get("path")}");
            }
            return paths;
        }
    }
}
