using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
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
            ClearBlogSelection();

            if (blogFiles.Count() <= 1)
            {
                DetailsViewModel.Count = 1;
                DetailsViewModel.BlogFile = blogFiles.FirstOrDefault();
            }
            else
            {
                DetailsViewModel.Count = blogFiles.Count();
                DetailsViewModel.BlogFile = CreateFromMultiple(blogFiles.ToArray());
                DetailsViewModel.BlogFile.PropertyChanged += ChangeBlogSettings;
            }
        }

        private void ChangeBlogSettings(object sender, PropertyChangedEventArgs e)
        {
            foreach (IBlog blog in blogsToSave)
            {
                PropertyInfo property = typeof(IBlog).GetProperty(e.PropertyName);
                property.SetValue(blog, property.GetValue(DetailsViewModel.BlogFile), null);
            }
        }

        public void Initialize()
        {
            ((INotifyCollectionChanged)selectionService.SelectedBlogFiles).CollectionChanged += SelectedBlogFilesCollectionChanged;
            shellService.DetailsView = DetailsViewModel.View;
        }

        public void Shutdown()
        {
        }

        public IBlog CreateFromMultiple(IEnumerable<IBlog> blogFiles)
        {
            if (!blogFiles.Any())
            {
                throw new ArgumentException("The collection must have at least one item.", nameof(blogFiles));
            }

            IBlog[] sharedBlogFiles = blogFiles.ToArray();
            foreach (IBlog blog in sharedBlogFiles)
            {
                blogsToSave.Add(blog);
            }

            return new Blog()
            {
                Name = string.Join(", ", sharedBlogFiles.Select(blog => blog.Name).ToArray()),
                Url = string.Join(", ", sharedBlogFiles.Select(blog => blog.Url).ToArray()),
                Posts = sharedBlogFiles.Sum(blogs => blogs.Posts),
                TotalCount = sharedBlogFiles.Sum(blogs => blogs.TotalCount),
                Texts = sharedBlogFiles.Sum(blogs => blogs.Texts),
                Answers = sharedBlogFiles.Sum(blogs => blogs.Answers),
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
                DownloadedAnswers = sharedBlogFiles.Sum(blogs => blogs.DownloadedAnswers),
                DownloadedVideos = sharedBlogFiles.Sum(blogs => blogs.DownloadedVideos),
                DownloadedAudios = sharedBlogFiles.Sum(blogs => blogs.DownloadedAudios),
                DownloadedPhotoMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedPhotoMetas),
                DownloadedVideoMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedVideoMetas),
                DownloadedAudioMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedAudioMetas),
                DownloadPages = SetDownloadPages(sharedBlogFiles),
                PageSize = SetProperty<int>(sharedBlogFiles, "PageSize"),
                DownloadAudio = SetCheckBox(sharedBlogFiles, "DownloadAudio"),
                DownloadConversation = SetCheckBox(sharedBlogFiles, "DownloadConversation"),
                DownloadLink = SetCheckBox(sharedBlogFiles, "DownloadLink"),
                DownloadPhoto = SetCheckBox(sharedBlogFiles, "DownloadPhoto"),
                DownloadQuote = SetCheckBox(sharedBlogFiles, "DownloadQuote"),
                DownloadText = SetCheckBox(sharedBlogFiles, "DownloadText"),
                DownloadAnswer = SetCheckBox(sharedBlogFiles, "DownloadAnswer"),
                DownloadVideo = SetCheckBox(sharedBlogFiles, "DownloadVideo"),
                CreatePhotoMeta = SetCheckBox(sharedBlogFiles, "CreatePhotoMeta"),
                CreateVideoMeta = SetCheckBox(sharedBlogFiles, "CreateVideoMeta"),
                CreateAudioMeta = SetCheckBox(sharedBlogFiles, "CreateAudioMeta"),
                DownloadRebloggedPosts = SetCheckBox(sharedBlogFiles, "DownloadRebloggedPosts"),
                SkipGif = SetCheckBox(sharedBlogFiles, "SkipGif"),
                ForceSize = SetCheckBox(sharedBlogFiles, "ForceSize"),
                ForceRescan = SetCheckBox(sharedBlogFiles, "ForceRescan"),
                CheckDirectoryForFiles = SetCheckBox(sharedBlogFiles, "CheckDirectoryForFiles"),
                DownloadUrlList = SetCheckBox(sharedBlogFiles, "DownloadUrlList"),
                Dirty = false
            };
        }

        private static T SetProperty<T>(IReadOnlyCollection<IBlog> blogs, string propertyName) where T : IConvertible
        {
            PropertyInfo property = typeof(IBlog).GetProperty(propertyName);
            var value = (T)property.GetValue(blogs.FirstOrDefault());
            bool equal = blogs.All(blog => property.GetValue(blog).Equals(value));
            if (equal)
                return value;
            return default(T);
        }

        private static string SetDownloadPages(IReadOnlyCollection<IBlog> blogs)
        {
            string downloadPages = blogs.FirstOrDefault().DownloadPages;
            bool equal = blogs.All(blog => blog.DownloadPages == downloadPages);
            if (equal)
                return downloadPages;
            return String.Empty;
        }

        private static bool SetCheckBox(IReadOnlyCollection<IBlog> blogs, string propertyName)
        {
            PropertyInfo property = typeof(IBlog).GetProperty(propertyName);
            int numberOfBlogs = blogs.Count;
            int checkedBlogs = blogs.Select(blog => (bool)property.GetValue(blog)).Count(state => state);
            if (checkedBlogs == numberOfBlogs)
                return true;
            if (checkedBlogs == 0)
                return false;
            //return null;
            return false;
        }

        private void ClearBlogSelection()
        {
            if (blogsToSave.Any())
            {
                blogsToSave.Clear();
            }
        }

        private void SelectedBlogFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DetailsViewModel.BlogFile != null)
            {
                DetailsViewModel.BlogFile.PropertyChanged -= ChangeBlogSettings;
            }
            SelectBlogFiles(selectionService.SelectedBlogFiles.Cast<Blog>().ToArray());
        }
    }
}
