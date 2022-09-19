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
        private StandardAnalyzer analyzer;
        private int indexingPercent = 0;
        private string userProfile;
        private int nestingLevel = 1;
        private string[] IndexedLocations = { "documents", "downloads" };

        private ContentIndexer()
        {
            // Construct a machine-independent path for the index
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
            StorageFolder documentsFolder = KnownFolders.DocumentsLibrary;
            StorageFolder downloadsFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\Downloads");
            List<Task> indexingTasks = new List<Task>();
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[0], documentsFolder);
                this.IndexingPercentage = this.indexingPercent + 25;
            }));
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[1], downloadsFolder);
                this.IndexingPercentage = this.indexingPercent + 25;
            }));
            Task.WaitAll(indexingTasks.ToArray());
            this.IndexingPercentage = 100;
        }

        public async Task IndexDataInBackground()
        {
            StorageFolder documentsFolder = KnownFolders.DocumentsLibrary;
            StorageFolder downloadsFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\Downloads");
            List<Task> indexingTasks = new List<Task>();
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[0], documentsFolder);
            }));
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[1], downloadsFolder);
            }));
        }

        public async Task IndexDataForLocation(string location, StorageFolder storageFolder)
        {
            string basePath = Environment.GetFolderPath(
              Environment.SpecialFolder.CommonApplicationData);
            string indexPath = Path.Combine(basePath, $"hyperXindex-{location}");
            using (var dir = FSDirectory.Open(indexPath))
            {
                var indexWriterConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer);
                indexWriterConfig.OpenMode = OpenMode.CREATE_OR_APPEND;
                using (var writer = new IndexWriter(dir, indexWriterConfig))
                {
                    try
                    {
                        await IndexLocationsFolder(writer, storageFolder);
                        SettingsContainer.instance.Value.SetValue<bool>("indexingComplete", true);
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

        private async Task IndexLocationsFolder(IndexWriter writer, StorageFolder storageFolder)
        {
            IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();
            foreach (StorageFile file in files)
            {
                var doc = new Document();
                doc.Add(new Field("name", file.Name.ToLower(), new FieldType { IsIndexed = true, IsStored = true, IndexOptions = IndexOptions.DOCS_ONLY }));
                doc.Add(new Field("path", file.Path, new FieldType { IsIndexed = false, IsStored = true }));
                Term t = new Term("name", file.Name.ToLower());
                writer.UpdateDocument(t, doc);
            }
            IList<StorageFolder> totalFolders = new List<StorageFolder>();
            IReadOnlyList<StorageFolder> folders = await storageFolder.GetFoldersAsync();
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
        }

        private void GetAllFolders(IList<StorageFolder> totalFolders, IReadOnlyList<StorageFolder> folders, int level)
        {
            totalFolders.AddRange(folders);
            if (level >= nestingLevel)
            {
                return;
            }
            foreach (StorageFolder f in folders)
            {
                var nestedFolders = f.GetFoldersAsync().GetResults();
                if (nestedFolders != null && nestedFolders.Count > 0)
                {
                    GetAllFolders(totalFolders, nestedFolders, level + 1);
                }
            }
        }

        public void DeleteAllIndexData()
        {
            foreach (string location in IndexedLocations)
            {
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string indexPath = Path.Combine(basePath, $"hyperXindex-{location}");
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
}
