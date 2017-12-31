using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using System.Waf.Foundation;

using TumblThree.Applications.Data;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class QueueController
    {
        private readonly DelegateCommand clearQueueCommand;
        private readonly ICrawlerService crawlerService;
        private readonly IDetailsService detailsService;
        private readonly IFileDialogService fileDialogService;
        private readonly IManagerService managerService;
        private readonly DelegateCommand openQueueCommand;
        private readonly FileType openQueuelistFileType;
        private readonly Lazy<QueueViewModel> queueViewModel;
        private readonly DelegateCommand removeSelectedCommand;
        private readonly DelegateCommand saveQueueCommand;
        private readonly FileType saveQueuelistFileType;
        private readonly IShellService shellService;
        private readonly DelegateCommand showBlogDetailsCommand;

        [ImportingConstructor]
        public QueueController(IFileDialogService fileDialogService, IShellService shellService, IDetailsService detailsService,
            IManagerService managerService, ICrawlerService crawlerService, Lazy<QueueViewModel> queueViewModel)
        {
            this.fileDialogService = fileDialogService;
            this.shellService = shellService;
            this.queueViewModel = queueViewModel;
            this.managerService = managerService;
            this.crawlerService = crawlerService;
            this.detailsService = detailsService;
            removeSelectedCommand = new DelegateCommand(RemoveSelected, CanRemoveSelected);
            showBlogDetailsCommand = new DelegateCommand(ShowBlogDetails);
            openQueueCommand = new DelegateCommand(OpenList);
            saveQueueCommand = new DelegateCommand(SaveList);
            clearQueueCommand = new DelegateCommand(ClearList);
            openQueuelistFileType = new FileType(Resources.Queuelist, SupportedFileTypes.QueueFileExtensions);
            saveQueuelistFileType = new FileType(Resources.Queuelist, SupportedFileTypes.QueueFileExtensions.First());
        }

        public QueueSettings QueueSettings { get; set; }

        public QueueManager QueueManager { get; set; }

        private QueueViewModel QueueViewModel
        {
            get { return queueViewModel.Value; }
        }

        public void Initialize()
        {
            QueueViewModel.QueueManager = QueueManager;
            QueueViewModel.RemoveSelectedCommand = removeSelectedCommand;
            QueueViewModel.ShowBlogDetailsCommand = showBlogDetailsCommand;
            QueueViewModel.OpenQueueCommand = openQueueCommand;
            QueueViewModel.SaveQueueCommand = saveQueueCommand;
            QueueViewModel.ClearQueueCommand = clearQueueCommand;
            QueueViewModel.InsertBlogFilesAction = InsertBlogFiles;

            crawlerService.RemoveBlogFromQueueCommand = removeSelectedCommand;

            QueueViewModel.PropertyChanged += QueueViewModelPropertyChanged;

            shellService.QueueView = QueueViewModel.View;
        }

        public void Run()
        {
        }

        public void LoadQueue()
        {
            IReadOnlyList<string> blogNamesToLoad = QueueSettings.Names;
            IReadOnlyList<BlogTypes> blogTypesToLoad = QueueSettings.Types;
            InsertFilesCore(0, blogNamesToLoad, blogTypesToLoad);
        }

        public void Shutdown()
        {
            QueueSettings.ReplaceAll(QueueManager.Items.Select(x => x.Blog.Name), QueueManager.Items.Select(x => x.Blog.BlogType));
        }

        private bool CanRemoveSelected()
        {
            return QueueViewModel.SelectedQueueItem != null;
        }

        private void RemoveSelected()
        {
            QueueListItem[] queueItemsToExclude =
                QueueViewModel.SelectedQueueItems.Except(new[] { QueueViewModel.SelectedQueueItem }).ToArray();
            QueueListItem nextQueueItem =
                CollectionHelper.GetNextElementOrDefault(QueueManager.Items.Except(queueItemsToExclude).ToArray(),
                    QueueViewModel.SelectedQueueItem);

            QueueManager.RemoveItems(QueueViewModel.SelectedQueueItems);
            QueueViewModel.SelectedQueueItem = nextQueueItem ?? QueueManager.Items.LastOrDefault();
        }

        private void ShowBlogDetails()
        {
            detailsService.SelectBlogFiles(QueueViewModel.SelectedQueueItems.Select(x => x.Blog).ToArray());
            shellService.ShowDetailsView();
        }

        private void InsertBlogFiles(int index, IEnumerable<IBlog> blogFiles)
        {
            QueueManager.InsertItems(index, blogFiles.Select(x => new QueueListItem(x)));
        }

        private void OpenList()
        {
            FileDialogResult result = fileDialogService.ShowOpenFileDialog(shellService.ShellView, openQueuelistFileType);
            if (!result.IsValid)
            {
                return;
            }
            OpenListCore(result.FileName);
        }

        private void OpenListCore(string queuelistFileName)
        {
            QueueSettings queueList;

            try
            {
                using (var stream = new FileStream(queuelistFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(QueueSettings));
                    queueList = (QueueSettings)serializer.ReadObject(stream);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("QueueController:OpenListCore: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotLoadQueuelist);
                return;
            }

            InsertFilesCore(QueueManager.Items.Count(), queueList.Names.ToArray(), queueList.Types.ToArray());
        }

        private void InsertFilesCore(int index, IEnumerable<string> names, IEnumerable<BlogTypes> blogTypes)
        {
            try
            {
                InsertBlogFiles(index, names.Zip(blogTypes, Tuple.Create).Select(x => managerService.BlogFiles.First(blogs => blogs.Name.Equals(x.Item1) && blogs.BlogType.Equals(x.Item2))));
            }
            catch (Exception ex)
            {
                Logger.Error("QueueController.InsertFileCore: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotLoadQueuelist);
            }
        }

        private void SaveList()
        {
            FileDialogResult result = fileDialogService.ShowSaveFileDialog(shellService.ShellView, saveQueuelistFileType);
            if (!result.IsValid)
            {
                return;
            }

            var queueList = new QueueSettings();
            queueList.ReplaceAll(QueueManager.Items.Select(item => item.Blog.Name).ToList(),
                QueueManager.Items.Select(item => item.Blog.BlogType).ToList());

            try
            {
                string targetFolder = Path.GetDirectoryName(result.FileName);
                string name = Path.GetFileNameWithoutExtension(result.FileName);

                using (
                    var stream = new FileStream(Path.Combine(targetFolder, name) + ".que", FileMode.Create, FileAccess.Write,
                        FileShare.None))
                {
                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(
                        stream, Encoding.UTF8, true, true, "  "))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(QueueSettings));
                        serializer.WriteObject(writer, queueList);
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("QueueController:SaveList: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotSaveQueueList);
            }
        }

        private void ClearList()
        {
            QueueManager.ClearItems();
        }

        private void QueueViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QueueViewModel.SelectedQueueItem))
            {
                UpdateCommands();
            }
        }

        private void UpdateCommands()
        {
            removeSelectedCommand.RaiseCanExecuteChanged();
        }
    }
}
