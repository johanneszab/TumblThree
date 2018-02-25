using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace TumblThree.Applications.Downloader
{
    public class GoogleDriveDownloader : IGoogleDriveDownloader
    {
        UserCredential credentials;
        DriveService service;

        public GoogleDriveDownloader()
        {
        }

        public void Authenticate()
        {
            string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.Waf.Applications.ApplicationInfo.Company, System.Waf.Applications.ApplicationInfo.ProductName, "Settings");
            string portablePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            string currentFolder = settingsPath ?? portablePath;
            string credentialsFolder = Path.Combine(currentFolder, "credential");
            string GoogleTokenPath = Path.Combine(credentialsFolder, "google_secret.json");

            if (!System.IO.File.Exists(GoogleTokenPath))
            {
                StreamWriter GoogleTokenFile = new StreamWriter(GoogleTokenPath);
                string info = "{\"installed\":{\"client_id\":\"137806895872-pqf0ebihtgtho6jhdiichgjhc2cql2ep.apps.googleusercontent.com\",\"project_id\":\"oceanic-guard-192311\",\"auth_uri\":\"https://accounts.google.com/o/oauth2/auth\",\"token_uri\":\"https://accounts.google.com/o/oauth2/token\",\"auth_provider_x509_cert_url\":\"https://www.googleapis.com/oauth2/v1/certs\",\"client_secret\":\"br7VoHXdA9v8uSOzJ_Tcvq1c\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\",\"http://localhost\"]}}";
                GoogleTokenFile.WriteLine(info);
                GoogleTokenFile.Close();
            }
            Authenticate(credentialsFolder, GoogleTokenPath);
        }

        private void Authenticate(string credentialsFolder, string tokenPath)
        {
            using (FileStream stream = new FileStream(tokenPath, FileMode.Open, FileAccess.Read))
            {
                // Delete credentials cache at folder debug/bin/credentials after changes here
                credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[]
                    {
                        DriveService.Scope.Drive,
                        DriveService.Scope.DriveAppdata,
                        DriveService.Scope.DriveFile,
                        DriveService.Scope.DriveMetadata,
                        DriveService.Scope.DriveScripts,
						//Google.Apis.Drive.v3.DriveService.Scope.DriveReadonly,
						DriveService.Scope.DrivePhotosReadonly
                    },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credentialsFolder, true)).Result;
            }
        }

        public void OpenService()
        {
            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials
            });
        }

        public string getIdFromUrl(string url)
        {
            string id = "";
            string[] parts = url.Split('/');

            if (url.IndexOf("?id=") >= 0)
            {
                id = (parts[3].Split('=')[1].Replace("&usp", ""));
                return id;
            }

            string[] tempid = parts[5].Split('/');

            List<string> sortList = tempid.OrderBy(a => a).ToList();
            id = sortList[0];
            return id;
        }

        public async Task<GoogleFile> RequestInfo(string url, string path)
        {
            try
            {
                string fileId = getIdFromUrl(url);
                FilesResource.GetRequest request = service.Files.Get(fileId);
                GoogleFile file = request.Execute();
                await downloadFile(file, path + "\\");
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }

            return null;
        }

        public async Task downloadFile(GoogleFile FileResource, string path)
        {
            if (FileResource.MimeType != "application/vnd.google-apps.folder")
            {
                MemoryStream stream = new MemoryStream();

                await service.Files.Get(FileResource.Id).DownloadAsync(stream);

                FileStream file = new FileStream(path + @"/" + FileResource.Name, FileMode.Create, FileAccess.Write);
                stream.WriteTo(file);
                file.Close();
            }
            else
            {
                string NewPath = path + @"/" + FileResource.Name;

                Directory.CreateDirectory(NewPath);
                List<GoogleFile> SubFolderItems = IterateFolder(FileResource.Id);

                foreach (GoogleFile Item in SubFolderItems)
                {
                    await downloadFile(Item, NewPath);
                }
            }
        }

        public List<GoogleFile> IterateFolder(string folderId)
        {
            List<GoogleFile> TList = new List<GoogleFile>();
            FilesResource.ListRequest request = service.Files.List();
            request.Q = $"'{folderId}' in parents";

            do
            {
                try
                {
                    FileList children = request.Execute();

                    foreach (GoogleFile child in children.Files)
                        TList.Add(service.Files.Get(child.Id).Execute());

                    request.PageToken = children.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.Write("An error occured:" + e.Message);
                    request.PageToken = null;
                }
            } while (!string.IsNullOrEmpty(request.PageToken));

            return TList;
        }
    }
}
