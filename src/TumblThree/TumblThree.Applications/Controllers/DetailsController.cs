using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export, Export(typeof(IDetailsService))]
    internal class DetailsController : IDetailsService
    {
        private readonly HashSet<IBlog> blogsToSave;
        private readonly Lazy<DetailsViewModel> detailsViewModel;
        private readonly IManagerService managerService;
        private readonly ISelectionService selectionService;
        private readonly IShellService shellService;

        [ImportingConstructor]
        public DetailsController(IShellService shellService, ISelectionService selectionService, IManagerService managerService,
            Lazy<DetailsViewModel> detailsViewModel)
        {
            this.shellService = shellService;
            this.selectionService = selectionService;
            this.managerService = managerService;
            this.detailsViewModel = detailsViewModel;
            blogsToSave = new HashSet<IBlog>();
        }

        public QueueManager QueueManager { get; set; }

        private DetailsViewModel DetailsViewModel
        {
            get { return detailsViewModel.Value; }
        }

        public void SelectBlogFiles(IReadOnlyList<IBlog> blogFiles)
        {
            // Save changes to previous selected files
            SaveCurrentSelectedFile();

            if (blogFiles.Count() <= 1)
            {
                DetailsViewModel.Count = 1;
                DetailsViewModel.BlogFile = blogFiles.Cast<TumblrBlog>().FirstOrDefault();
            }
            else
            {
                DetailsViewModel.Count = blogFiles.Count();
                DetailsViewModel.BlogFile = CreateFromMultiple(blogFiles.Cast<TumblrBlog>().ToArray());
            }
        }

        public void Initialize()
        {
            ((INotifyCollectionChanged)selectionService.SelectedBlogFiles).CollectionChanged += SelectedBlogFilesCollectionChanged;
            shellService.DetailsView = DetailsViewModel.View;
        }

        public void Shutdown()
        {
            Task task = Task.Run(() => SaveCurrentSelectedFile());
            shellService.AddTaskToCompleteBeforeShutdown(task);
        }

        public TumblrBlog CreateFromMultiple(IEnumerable<TumblrBlog> blogFiles)
        {
            if (!blogFiles.Any())
            {
                throw new ArgumentException("The collection must have at least one item.", nameof(blogFiles));
            }

            TumblrBlog[] sharedBlogFiles = blogFiles.Cast<TumblrBlog>().ToArray();
            foreach (Blog blog in sharedBlogFiles)
            {
                blogsToSave.Add(blog);
            }

            return new TumblrBlog()
            {
                Name = string.Join(", ", sharedBlogFiles.Select(blog => blog.Name).ToArray()),
                Url = string.Join(", ", sharedBlogFiles.Select(blog => blog.Url).ToArray()),
                Posts = sharedBlogFiles.Sum(blogs => blogs.Posts),
                TotalCount = sharedBlogFiles.Sum(blogs => blogs.TotalCount),
                Texts = sharedBlogFiles.Sum(blogs => blogs.Texts),
                Quotes = sharedBlogFiles.Sum(blogs => blogs.Quotes),
                Photos = sharedBlogFiles.Sum(blogs => blogs.Photos),
                NumberOfLinks = sharedBlogFiles.Sum(blogs => blogs.NumberOfLinks),
                Conversations = sharedBlogFiles.Sum(blogs => blogs.Conversations),
                Videos = sharedBlogFiles.Sum(blogs => blogs.Videos),
                Audios = sharedBlogFiles.Sum(blogs => blogs.Audios),
                PhotoMetas = sharedBlogFiles.Sum(blogs => blogs.PhotoMetas),
                VideoMetas = sharedBlogFiles.Sum(blogs => blogs.VideoMetas),
                AudioMetas = sharedBlogFiles.Sum(blogs => blogs.AudioMetas),
                DownloadedTexts = sharedBlogFiles.Sum(blogs => blogs.DownloadedTexts),
                DownloadedQuotes = sharedBlogFiles.Sum(blogs => blogs.DownloadedQuotes),
                DownloadedPhotos = sharedBlogFiles.Sum(blogs => blogs.DownloadedPhotos),
                DownloadedLinks = sharedBlogFiles.Sum(blogs => blogs.DownloadedLinks),
                DownloadedConversations = sharedBlogFiles.Sum(blogs => blogs.DownloadedConversations),
                DownloadedVideos = sharedBlogFiles.Sum(blogs => blogs.DownloadedVideos),
                DownloadedAudios = sharedBlogFiles.Sum(blogs => blogs.DownloadedAudios),
                DownloadedPhotoMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedPhotoMetas),
                DownloadedVideoMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedVideoMetas),
                DownloadedAudioMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedAudioMetas),
                DownloadAudio = false,
                DownloadConversation = false,
                DownloadLink = false,
                DownloadPhoto = false,
                DownloadQuote = false,
                DownloadText = false,
                DownloadVideo = false,
                CreatePhotoMeta = false,
                CreateVideoMeta = false,
                CreateAudioMeta = false,
                SkipGif = false,
                ForceSize = false,
                ForceRescan = false,
                CheckDirectoryForFiles = false,
                DownloadUrlList = false,
                Dirty = false
            };
        }

        private void SaveCurrentSelectedFile()
        {
            SaveChanges(DetailsViewModel.BlogFile);
        }

        private void SaveChanges(TumblrBlog blogFile)
        {
            if (blogFile == null)
            {
                return;
            }
            IReadOnlyCollection<IBlog> filesToSave;
            if (blogsToSave.Any())
            {
                filesToSave = managerService.BlogFiles.Where(blogs => blogsToSave.Contains(blogs)).ToArray();
            }
            else
            {
                filesToSave = new[] { blogFile };
            }

            if (blogFile.Dirty)
            {
                foreach (TumblrBlog blog in filesToSave)
                {
                    blog.DownloadAudio = blogFile.DownloadAudio;
                    blog.DownloadConversation = blogFile.DownloadConversation;
                    blog.DownloadLink = blogFile.DownloadLink;
                    blog.DownloadPhoto = blogFile.DownloadPhoto;
                    blog.DownloadQuote = blogFile.DownloadQuote;
                    blog.DownloadText = blogFile.DownloadText;
                    blog.DownloadVideo = blogFile.DownloadVideo;
                    blog.CreatePhotoMeta = blogFile.CreatePhotoMeta;
                    blog.CreateVideoMeta = blogFile.CreateVideoMeta;
                    blog.CreateAudioMeta = blogFile.CreateAudioMeta;
                    blog.SkipGif = blogFile.SkipGif;
                    blog.ForceSize = blogFile.ForceSize;
                    blog.ForceRescan = blogFile.ForceRescan;
                    blog.CheckDirectoryForFiles = blogFile.CheckDirectoryForFiles;
                    blog.DownloadUrlList = blogFile.DownloadUrlList;
                    blog.Dirty = true;
                }
            }

            RemoveBlogFilesToSave(filesToSave);
        }

        private void RemoveBlogFilesToSave(IEnumerable<IBlog> blogFiles)
        {
            foreach (IBlog x in blogFiles)
            {
                blogsToSave.Remove(x);
            }
        }

        private void SelectedBlogFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SelectBlogFiles(selectionService.SelectedBlogFiles.Cast<Blog>().ToArray());
        }
    }
}
