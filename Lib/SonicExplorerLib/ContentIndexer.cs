﻿using DynamicData;
using Lucene.Net.Analysis.Core;
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
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using static System.Net.WebRequestMethods;
using Path = System.IO.Path;

namespace SonicExplorerLib
{
    public class ContentIndexer : ReactiveObject
    {
        private static Lazy<ContentIndexer> Instance = new Lazy<ContentIndexer>(() => new ContentIndexer());

        public static ContentIndexer GetInstance => Instance.Value;
        private KeywordAnalyzer analyzer;
        private int indexingPercent = 0;
        private string userProfile;
        private const int nestingLevel = 1;
        private string[] IndexedLocations = { "documents", "downloads", "desktop", "pictures", "music", "videos" };

        private ContentIndexer()
        {
            // Construct a machine-independent path for the index
            analyzer = new KeywordAnalyzer();
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
            try
            {
                StorageFolder documentsFolder = KnownFolders.DocumentsLibrary;
                StorageFolder picturesFolder = KnownFolders.PicturesLibrary;
                StorageFolder musicFolder = KnownFolders.MusicLibrary;
                StorageFolder videosFolder = KnownFolders.VideosLibrary;
                StorageFolder desktopFolder = null;
                StorageFolder downloadsFolder = null;
                try
                {
                    downloadsFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\Downloads");
                } catch (Exception)
                {
                    // do nothing;
                }
                try
                {
                    desktopFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\Desktop");
                }
                catch (Exception)
                {
                    try
                    {
                        desktopFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\OneDrive\\Desktop");
                    }
                    catch (Exception)
                    {
                        try
                        {
                            desktopFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\OneDrive - Microsoft\\Desktop");
                        } catch (Exception)
                        {
                            // do nothing;
                        }
                    }
                }
                List<Task> indexingTasks = new List<Task>();
                indexingTasks.Add(Task.Run(async () =>
                {
                    await IndexDataForLocation(IndexedLocations[0], documentsFolder);
                    this.IndexingPercentage = this.indexingPercent + 16;
                }));
                if (downloadsFolder != null)
                {
                    indexingTasks.Add(Task.Run(async () =>
                    {
                        await IndexDataForLocation(IndexedLocations[1], downloadsFolder);
                        this.IndexingPercentage = this.indexingPercent + 16;
                    }));
                }
                if (desktopFolder != null)
                {
                    indexingTasks.Add(Task.Run(async () =>
                    {
                        await IndexDataForLocation(IndexedLocations[2], desktopFolder);
                        this.IndexingPercentage = this.indexingPercent + 16;
                    }));
                }
                indexingTasks.Add(Task.Run(async () =>
                {
                    await IndexDataForLocation(IndexedLocations[3], picturesFolder);
                    this.IndexingPercentage = this.indexingPercent + 16;
                }));
                indexingTasks.Add(Task.Run(async () =>
                {
                    await IndexDataForLocation(IndexedLocations[4], musicFolder);
                    this.IndexingPercentage = this.indexingPercent + 16;
                }));
                indexingTasks.Add(Task.Run(async () =>
                {
                    await IndexDataForLocation(IndexedLocations[5], videosFolder);
                    this.IndexingPercentage = this.indexingPercent + 16;
                }));
                Task.WaitAll(indexingTasks.ToArray());
                this.IndexingPercentage = 100;
                SettingsContainer.instance.Value.SetValue<bool>("indexingComplete", true);
                SearchResultService.instance.RefreshSearch();
                SettingsContainer.instance.Value.SetValue<DateTimeOffset>("indexingTime", DateTimeOffset.Now);
            }
            catch (UnauthorizedAccessException)
            {
                _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
                });
            } catch (Exception e)
            {
                // Catching generic exceptions;
            }
        }

        public async Task IndexDataInBackground()
        {
            DeleteAllIndexData();
            StorageFolder downloadsFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\Downloads");
            StorageFolder documentsFolder = KnownFolders.DocumentsLibrary;
            StorageFolder picturesFolder = KnownFolders.PicturesLibrary;
            StorageFolder musicFolder = KnownFolders.MusicLibrary;
            StorageFolder videosFolder = KnownFolders.VideosLibrary;
            StorageFolder desktopFolder;
            try
            {
                desktopFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\Desktop");
            }
            catch (Exception)
            {
                try
                {
                    desktopFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\OneDrive\\Desktop");
                }
                catch (Exception)
                {
                    desktopFolder = await StorageFolder.GetFolderFromPathAsync($"{userProfile}\\OneDrive - Microsoft\\Desktop");
                }
            }
            List<Task> indexingTasks = new List<Task>();
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[0], documentsFolder);
            }));
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[1], downloadsFolder);
            }));
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[2], desktopFolder);
            }));
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[3], picturesFolder);
            }));
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[4], musicFolder);
            }));
            indexingTasks.Add(Task.Run(async () =>
            {
                await IndexDataForLocation(IndexedLocations[5], videosFolder);
            }));
            Task.WaitAll(indexingTasks.ToArray());
            SettingsContainer.instance.Value.SetValue<bool>("indexingComplete", true);
            SettingsContainer.instance.Value.SetValue<DateTimeOffset>("indexingTime", DateTimeOffset.Now);
        }

        private async Task IndexDataForLocation(string location, StorageFolder storageFolder)
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
                doc.Add(new Field("displayName", file.Name, new FieldType { IsIndexed = false, IsStored = true }));
                Term t = new Term("name", file.Name.ToLower());
                writer.UpdateDocument(t, doc);
            }
            writer.Flush(triggerMerge: true, applyAllDeletes: true);
            IList<StorageFolder> totalFolders = new List<StorageFolder>();
            IReadOnlyList<StorageFolder> folders = await storageFolder.GetFoldersAsync();
            if (folders == null || folders.Count == 0)
            {
                return;
            }
            GetAllFolders(totalFolders, folders, 0);
            foreach (StorageFolder folder in totalFolders)
            {
                var folderDoc = new Document();
                folderDoc.Add(new Field("name", folder.Name.ToLower(), new FieldType { IsIndexed = true, IsStored = true, IndexOptions = IndexOptions.DOCS_ONLY }));
                folderDoc.Add(new Field("path", folder.Path, new FieldType { IsIndexed = false, IsStored = true }));
                folderDoc.Add(new Field("displayName", folder.Name, new FieldType { IsIndexed = false, IsStored = true }));
                folderDoc.Add(new Field("folder", "true", new FieldType { IsIndexed = false, IsStored = true }));
                Term folderTerm = new Term("name", folder.Name.ToLower());
                writer.UpdateDocument(folderTerm, folderDoc);

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
                    doc.Add(new Field("displayName", file.Name, new FieldType { IsIndexed = false, IsStored = true }));
                    Term t = new Term("name", file.Name.ToLower());
                    writer.UpdateDocument(t, doc);
                }
                writer.Flush(triggerMerge: true, applyAllDeletes: true);
            }
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
            SettingsContainer.instance.Value.SetValue<bool>("indexingComplete", false);
        }
    }
}
