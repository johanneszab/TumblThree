using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using System.Waf.Applications;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.DataModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using System.ComponentModel;
using TumblThree.Applications.Properties;
using TumblThree.Domain.Queue;
using System.Waf.Foundation;
using System.Waf.Applications.Services;
using TumblThree.Applications.Data;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class QueueController
    {
        private readonly IFileDialogService fileDialogService;
        private readonly IShellService shellService;
        private readonly ISelectionService selectionService;
        private readonly IEnvironmentService environmentService;
        private readonly CrawlerService crawlerService;
        private readonly Lazy<QueueViewModel> queueViewModel;
        private readonly DelegateCommand removeSelectedCommand;
        private readonly DelegateCommand showBlogPropertiesCommand;
        private readonly DelegateCommand openQueueCommand;
        private readonly DelegateCommand saveQueueCommand;
        private readonly DelegateCommand clearQueueCommand;
        private readonly FileType openQueuelistFileType;
        private readonly FileType saveQueuelistFileType;


        [ImportingConstructor]
        public QueueController(IFileDialogService fileDialogService, IShellService shellService, IEnvironmentService environmentService, CrawlerService crawlerService,
             ISelectionService selectionService, Lazy<QueueViewModel> queueViewModel)
        {
            this.fileDialogService = fileDialogService;
            this.shellService = shellService;
            this.queueViewModel = queueViewModel;
            this.environmentService = environmentService;
            this.crawlerService = crawlerService;
            this.selectionService = selectionService;
            this.removeSelectedCommand = new DelegateCommand(RemoveSelected, CanRemoveSelected);
            this.showBlogPropertiesCommand = new DelegateCommand(ShowBlogProperties);
            this.openQueueCommand = new DelegateCommand(OpenList);
            this.saveQueueCommand = new DelegateCommand(SaveList);
            this.clearQueueCommand = new DelegateCommand(ClearList);
            this.openQueuelistFileType = new FileType(Resources.Queuelist, SupportedFileTypes.QueueFileExtensions);
            this.saveQueuelistFileType = new FileType(Resources.Queuelist, SupportedFileTypes.QueueFileExtensions.First());
        }

        public QueueSettings QueueSettings { get; set; }

        public QueueManager QueueManager { get; set; }

        private QueueViewModel QueueViewModel { get { return queueViewModel.Value; } }


        public void Initialize()
        {
            QueueViewModel.QueueManager = QueueManager;
            QueueViewModel.RemoveSelectedCommand = removeSelectedCommand;
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
            IReadOnlyList<string> blogFilesToLoad;
            if (environmentService.QueueList.Any())
            {
                blogFilesToLoad = environmentService.QueueList;
            }
            else
            {
                blogFilesToLoad = QueueSettings.Names;
            }
            InsertFilesCore(0, blogFilesToLoad);
        }

        public void Shutdown()
        {
            QueueSettings.ReplaceAll(QueueManager.Items.Select(x => x.Blog.Name));
        }

        private bool CanRemoveSelected()
        {
            return QueueViewModel.SelectedQueueItem != null;
        }

        private void RemoveSelected()
        {
            var queueItemsToExclude = QueueViewModel.SelectedQueueItems.Except(new[] { QueueViewModel.SelectedQueueItem }).ToArray();
            QueueListItem nextQueueItem = CollectionHelper.GetNextElementOrDefault(QueueManager.Items.Except(queueItemsToExclude).ToArray(),
                QueueViewModel.SelectedQueueItem);

            QueueManager.RemoveItems(QueueViewModel.SelectedQueueItems);
            QueueViewModel.SelectedQueueItem = nextQueueItem ?? QueueManager.Items.LastOrDefault();
        }

        private void ShowBlogProperties()
        {
            shellService.ShowDetailsView();
        }

        private void InsertBlogFiles(int index, IEnumerable<IBlog> blogFiles)
        {
            QueueManager.InsertItems(index, blogFiles.Select(x => new QueueListItem(x)));
        }

        private void OpenList()
        {
            var result = fileDialogService.ShowOpenFileDialog(shellService.ShellView, openQueuelistFileType);
            if (!result.IsValid)
            {
                return;
            }
            OpenListCore(result.FileName);
        }

        private void OpenListCore(string queuelistFileName)
        {
            List<string> queueList;
            try
            {

                using (FileStream stream = new FileStream(queuelistFileName,
                    FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    IFormatter formatter = new BinaryFormatter();
                    queueList = (List<string>)formatter.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("OpenListCore: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotLoadQueuelist);
                return;
            }

            InsertFilesCore(QueueManager.Items.Count(), queueList.ToArray());
        }

        private void InsertFilesCore(int index, IEnumerable<string> fileNames)
        {
            try
            {
                InsertBlogFiles(index, fileNames.Select(x => selectionService.BlogFiles.First(blogs => blogs.Name.Contains(x))));
            }
            catch (Exception ex)
            {
                Logger.Error("QueueController.InsertFileCore: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotLoadQueuelist);
                return;
            }
        }

        private void SaveList()
        {
            var result = fileDialogService.ShowSaveFileDialog(shellService.ShellView, saveQueuelistFileType);
            if (!result.IsValid)
            {
                return;
            }

            var queueList = new List<string>();

            foreach (var item in QueueManager.Items)
            {
                queueList.Add(item.Blog.Name);
            }

            try
            {
                var targetFolder = Path.GetDirectoryName(result.FileName);
                var name = Path.GetFileNameWithoutExtension(result.FileName);

                using (FileStream stream = new FileStream(targetFolder + name + ".que", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, queueList);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("SaveList SaveAs: {0}", ex);
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
