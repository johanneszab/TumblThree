using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export, Export(typeof(IDetailsService))]
    internal class DetailsController : IDetailsService
    {
        private readonly IShellService shellService;
        private readonly ISelectionService selectionService;
        private readonly Lazy<DetailsViewModel> detailsViewModel;
        private readonly HashSet<IBlog> blogsToSave;

        [ImportingConstructor]
        public DetailsController(IShellService shellService, ISelectionService selectionService, Lazy<DetailsViewModel> detailsViewModel)
        {
            this.shellService = shellService;
            this.selectionService = selectionService;
            this.detailsViewModel = detailsViewModel;
            this.blogsToSave = new HashSet<IBlog>();
        }

        public QueueManager QueueManager { get; set; }

        private DetailsViewModel DetailsViewModel { get { return detailsViewModel.Value; } }

        public void Initialize()
        {
            //QueueManager.PropertyChanged += QueueManagerPropertyChanged;
            ((INotifyCollectionChanged)selectionService.SelectedBlogFiles).CollectionChanged += SelectedBlogFilesCollectionChanged;
            shellService.DetailsView = DetailsViewModel.View;
        }

        public void Shutdown()
        {
            var task = SaveCurrentSelectedFileAsync();
            shellService.AddTaskToCompleteBeforeShutdown(task);
        }

        public void SelectBlogFiles(IReadOnlyList<IBlog> blogFiles)
        {
            // Save changes to previous selected files
            SaveCurrentSelectedFileAsync();

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

        public TumblrBlog CreateFromMultiple(IEnumerable<TumblrBlog> blogFiles)
        {
            if (blogFiles.Count() < 1) { throw new ArgumentException("The collection must have at least one item.", "blogFiles"); }

            var sharedBlogFiles = blogFiles.Cast<TumblrBlog>().ToArray();
            foreach (Blog blog in sharedBlogFiles)
            {
                blogsToSave.Add(blog);
            }

            //FIXME: Create proper binding to Selectionservice List<Blogs>.
            return new TumblrBlog()
            {
                Name = string.Join(", ", sharedBlogFiles.Select(blog => blog.Name).ToArray()),
                Url = string.Join(", ", sharedBlogFiles.Select(blog => blog.Url).ToArray()),

                Posts = (uint)sharedBlogFiles.Sum(blogs => blogs.Posts),
                TotalCount = (uint)sharedBlogFiles.Sum(blogs => blogs.TotalCount),
                Texts = (uint)sharedBlogFiles.Sum(blogs => blogs.Texts),
                Quotes = (uint)sharedBlogFiles.Sum(blogs => blogs.Quotes),
                Photos = (uint)sharedBlogFiles.Sum(blogs => blogs.Photos),
                NumberOfLinks = (uint)sharedBlogFiles.Sum(blogs => blogs.NumberOfLinks),
                Conversations = (uint)sharedBlogFiles.Sum(blogs => blogs.Conversations),
                Videos = (uint)sharedBlogFiles.Sum(blogs => blogs.Videos),
                Audios = (uint)sharedBlogFiles.Sum(blogs => blogs.Audios),
                PhotoMetas = (uint)sharedBlogFiles.Sum(blogs => blogs.PhotoMetas),
                VideoMetas = (uint)sharedBlogFiles.Sum(blogs => blogs.VideoMetas),
                AudioMetas = (uint)sharedBlogFiles.Sum(blogs => blogs.AudioMetas),

                DownloadedTexts = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedTexts),
                DownloadedQuotes = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedQuotes),
                DownloadedPhotos = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedPhotos),
                DownloadedLinks = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedLinks),
                DownloadedConversations = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedConversations),
                DownloadedVideos = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedVideos),
                DownloadedAudios = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedAudios),
                DownloadedPhotoMetas = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedPhotoMetas),
                DownloadedVideoMetas = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedVideoMetas),
                DownloadedAudioMetas = (uint)sharedBlogFiles.Sum(blogs => blogs.DownloadedAudioMetas),

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
                Dirty = false
            };
        }

        private Task SaveCurrentSelectedFileAsync()
        {
            //musicFileContext.ApplyChanges(DetailsViewModel.BlogFile);
            return SaveChangesAsync(DetailsViewModel.BlogFile);
        }

        private async Task SaveChangesAsync(TumblrBlog blogFile)
        {
            if (blogFile == null)
            {
                return;
            }
            IReadOnlyCollection<IBlog> filesToSave;
            if (blogsToSave.Any())
            {
                filesToSave = selectionService.BlogFiles.Where(blogs => blogsToSave.Contains(blogs)).ToArray();
            }
            else
            {
                filesToSave = new[] { blogFile };
            }

            if (blogFile.Dirty)
            {
                //var tasks = Task.Run(() => Parallel.ForEach(filesToSave, blog =>
                //{
                //    blog.DownloadAudio = blogFile.DownloadAudio;
                //    blog.DownloadConversation = blogFile.DownloadConversation;
                //    blog.DownloadLink = blogFile.DownloadLink;
                //    blog.DownloadPhoto = blogFile.DownloadPhoto;
                //    blog.DownloadQuote = blogFile.DownloadQuote;
                //    blog.DownloadText = blogFile.DownloadText;
                //    blog.DownloadVideo = blogFile.DownloadVideo;
                //    blog.SkipGif = blogFile.SkipGif;
                //    blog.Dirty = true;
                //}));

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
                    blog.Dirty = true;                
                }
            }

            //try
            //{
            //    await Task.WhenAll(tasks);
            //}
            //catch (Exception ex)
            //{
            //    Logger.Error("SaveChangesAsync: {0}", ex);
            //    if (filesToSave.Count() == 1)
            //    {
            //        shellService.ShowError(ex, Resources.CouldNotSaveBlog, filesToSave.First().Name);
            //    }
            //    else
            //    {
            //        shellService.ShowError(ex, Resources.CouldNotSaveBlog);
            //    }
            //}
            //finally
            //{
                RemoveBlogFilesToSave(filesToSave);
            //}
        }

        private void RemoveBlogFilesToSave(IEnumerable<IBlog> blogFiles)
        {
            foreach (var x in blogFiles) { blogsToSave.Remove(x); }
        }

        private void SelectedBlogFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SelectBlogFiles(selectionService.SelectedBlogFiles.Cast<Blog>().ToArray());
        }
    }
}
