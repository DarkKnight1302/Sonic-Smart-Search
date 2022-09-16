using DynamicData;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;
using static Lucene.Net.Util.Packed.PackedInt32s;
using Path = System.IO.Path;

namespace SonicExplorerLib
{
    public class ContentIndexer : ReactiveObject
    {
        private static Lazy<ContentIndexer> Instance = new Lazy<ContentIndexer>(() => new ContentIndexer());

        public static ContentIndexer GetInstance => Instance.Value;
        private object indexLock = new object();
        private string basePath;
        private string indexPath;
        private StandardAnalyzer analyzer;
        private int indexingPercent = 0;
        private string userProfile;
        private int nestingLevel = 1;

        private ContentIndexer()
        {
            // Construct a machine-independent path for the index
            basePath = Environment.GetFolderPath(
               Environment.SpecialFolder.CommonApplicationData);
            indexPath = Path.Combine(basePath, "hyperXindex");
            analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public IObservable<int> IndexingPercentageObservable => this.WhenAnyValue(x => x.IndexingPercentage);

        private int IndexingPercentage
        {
            get => this.indexingPercent;
            set
            {
                _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        this.RaiseAndSetIfChanged(ref this.indexingPercent, value);
                    });
            }
        }

        public async Task IndexData()
        {
            using (var dir = FSDirectory.Open(indexPath))
            {
                var indexWriterConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer);
                indexWriterConfig.OpenMode = OpenMode.CREATE_OR_APPEND;
                using (var writer = new IndexWriter(dir, indexWriterConfig))
                {
                    try
                    {
                        List<Task> indexingTasks = new List<Task>();
                        indexingTasks.Add(Task.Run(async () => await IndexDocumentsFolder(writer).ConfigureAwait(false)));
                        indexingTasks.Add(Task.Run(async () => await IndexDownloadsFolder(writer).ConfigureAwait(false)));
                        await Task.WhenAll(indexingTasks);
                        this.IndexingPercentage = 100;
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        async () =>
                            {
                                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
                            });
                    }
                }
            }
        }

        private async Task IndexDocumentsFolder(IndexWriter writer)
        {
            StorageFolder Documentsfolder = KnownFolders.DocumentsLibrary;
            IReadOnlyList<StorageFile> files = await Documentsfolder.GetFilesAsync();
            foreach (StorageFile file in files)
            {
                var doc = new Document();
                doc.Add(new Field("name", file.Name.ToLower(), new FieldType { IsIndexed = true, IsStored = true, IndexOptions = IndexOptions.DOCS_ONLY }));
                doc.Add(new Field("path", file.Path, new FieldType { IsIndexed = false, IsStored = true }));
                Term t = new Term("name", file.Name.ToLower());
                writer.UpdateDocument(t, doc);
            }
            IList<StorageFolder> totalFolders = new List<StorageFolder>();
            IReadOnlyList<StorageFolder> folders = await Documentsfolder.GetFoldersAsync();
            if (folders == null || folders.Count == 0)
            {
                writer.Flush(triggerMerge: true, applyAllDeletes: true);
                return;
            }
            GetAllFolders(totalFolders, folders, 0);
            foreach (StorageFolder folder in totalFolders)
            {
                IReadOnlyList<StorageFile> Nestedfiles = await folder.GetFilesAsync();
                if (Nestedfiles == null || Nestedfiles.Count == 0)
                {
                    continue;
                }

                foreach (StorageFile file in Nestedfiles)
                {
                    var doc = new Document();
                    doc.Add(new Field("name", file.Name.ToLower(), new FieldType { IsIndexed = true, IsStored = true, IndexOptions = IndexOptions.DOCS_ONLY }));
                    doc.Add(new Field("path", file.Path, new FieldType { IsIndexed = false, IsStored = true }));
                    Term t = new Term("name", file.Name.ToLower());
                    writer.UpdateDocument(t, doc);
                }
            }
            writer.Flush(triggerMerge: true, applyAllDeletes: true);
            this.IndexingPercentage = indexingPercent + 25;
        }

        private void GetAllFolders(IList<StorageFolder> totalFolders, IReadOnlyList<StorageFolder> folders, int level)
        {
            totalFolders.AddRange(folders);
            if (level >= nestingLevel)
            {
                return;
            }
            foreach(StorageFolder f in folders)
            {
                var nestedFolders = f.GetFoldersAsync().GetResults();
                if (nestedFolders != null && nestedFolders.Count > 0)
                {
                    GetAllFolders(totalFolders, nestedFolders, level+1);
                }
            }
        }

        private async Task IndexDownloadsFolder(IndexWriter writer)
        {
            StorageFolder downloadsFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\Downloads");
            IReadOnlyList<StorageFile> files = await downloadsFolder.GetFilesAsync();
            foreach (StorageFile file in files)
            {
                var doc = new Document();
                doc.Add(new Field("name", file.Name.ToLower(), new FieldType { IsIndexed = true, IsStored = true, IndexOptions = IndexOptions.DOCS_ONLY }));
                doc.Add(new Field("path", file.Path, new FieldType { IsIndexed = false, IsStored = true }));
                Term t = new Term("name", file.Name.ToLower());
                writer.UpdateDocument(t, doc);
            }
            IList<StorageFolder> totalFolders = new List<StorageFolder>();
            IReadOnlyList<StorageFolder> folders = await downloadsFolder.GetFoldersAsync();
            if (folders == null || folders.Count == 0)
            {
                writer.Flush(triggerMerge: true, applyAllDeletes: true);
                return;
            }
            GetAllFolders(totalFolders, folders, 0);
            foreach (StorageFolder folder in totalFolders)
            {
                IReadOnlyList<StorageFile> Nestedfiles = await folder.GetFilesAsync();
                foreach (StorageFile file in Nestedfiles)
                {
                    var doc = new Document();
                    doc.Add(new Field("name", file.Name.ToLower(), new FieldType { IsIndexed = true, IsStored = true, IndexOptions = IndexOptions.DOCS_ONLY }));
                    doc.Add(new Field("path", file.Path, new FieldType { IsIndexed = false, IsStored = true }));
                    Term t = new Term("name", file.Name.ToLower());
                    writer.UpdateDocument(t, doc);
                }
                writer.Flush(triggerMerge: true, applyAllDeletes: true);
            }
            this.IndexingPercentage = indexingPercent + 25;
        }

        public async Task IndexDataInBackground()
        {
            using (var dir = FSDirectory.Open(indexPath))
            {
                var indexWriterConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer);
                indexWriterConfig.OpenMode = OpenMode.CREATE_OR_APPEND;
                using (var writer = new IndexWriter(dir, indexWriterConfig))
                {
                    try
                    {
                        StorageFolder Documentsfolder = KnownFolders.DocumentsLibrary;
                        StorageFolder downloadsFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\Downloads");
                        //IReadOnlyList<StorageFile> files =  await Documentsfolder.GetFilesAsync();
                        IReadOnlyList<StorageFile> files = await downloadsFolder.GetFilesAsync();
                        foreach (StorageFile file in files)
                        {
                            var doc = new Document();
                            doc.Add(new Field("name", file.Name.ToLower(), new FieldType { IsIndexed = true, IsStored = true, IndexOptions = IndexOptions.DOCS_ONLY }));
                            doc.Add(new Field("path", file.Path, new FieldType { IsIndexed = false, IsStored = true }));
                            Term t = new Term("name", file.Name.ToLower());
                            writer.UpdateDocument(t, doc);
                        }
                        writer.Flush(triggerMerge: true, applyAllDeletes: true);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        //do nothing.
                    }
                }
            }
        }

        public void DeleteAllIndexData()
        {
            using (var dir = FSDirectory.Open(indexPath))
            {
                var indexWriterConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer);
                indexWriterConfig.OpenMode = OpenMode.CREATE_OR_APPEND;
                using (var writer = new IndexWriter(dir, indexWriterConfig))
                {
                    writer.DeleteAll();
                    writer.Commit();
                }
            }
        }
    }
}
