# TumblThree - A Tumblr Blog Backup Application

TumblThree is the code rewrite of [TumblTwo](https://github.com/johanneszab/TumblTwo), a free and open source Tumblr blog backup application, using C# with WPF and the MVVM pattern. It uses the [Win Application Framework (WAF)](https://github.com/jbe2277/waf). It downloads photo, video, audio and text posts from a given tumblr blog.

### New Features (over TumblTwo):
* Internationalization support.
* Autosave of the queuelist.
* Save, clear and restore the queuelist.
* Download of text, audio, quote, conversation, link and question posts.
* Download meta information for photo, video and audio posts.
* Downloads inlined photos and videos (e.g. photos embedded in question&answer posts).
* Download of \_raw image files (original/higher resolution pictures). 
* A downloader for private blogs (login required blogs).
* A downloader for downloading "liked by" photos and videos instead of a tumblr blog.
* An option to download an url list instead of the actual files.
* Allows to download only original content of the blog and skip reblogged posts.
* Set a time interval for a automatic download (e.g. during nights).
* Can download only specific blog pages instead of the whole blog.
* Uses SSL instead of unsecure http connections.
* Allows to set a proxy.
* A bandwidth throttler.
* Preview of photos & videos.
* Taskbar buttons and key bindings.

### New Features (over TumblOne):
* Multiple simultaneous downloads of a single blog.
* Multiple simultaneous downloads of different blogs, customizable in the settings.
* Download of tumblr.com hosted videos.
* It is possible to download images from blogs only for specific tags.
* A clipboard monitor that detects *http(s):// .tumblr.com* urls in the clipboard (copy and paste) and automatically adds the blog to the bloglist.
* A download queue for blogs.
* A detection if the blog is still online or the owner has changed.
* The blogview is now sortable and shows more information, e.g. date added, last time finished and the progress.
* A settings panel (change download location, turn preview off/on, define number of simultaneous downloads, set the imagesize of downloaded pictures, etc.).
* Source code at github (Written in C# using WPF and MVVM).

## Screenshot:
![TumblThree Main UI](http://www.jzab.de/sites/default/files/images/tumblthree.png?raw=true "TumblThree Main UI")

### Application Usage: ###

* Usage
  * extract the .zip file and run the application by double clicking TumblThree.exe. The application now comes as a zip file as some parts of it are modular .dll files like internationalization support.
  * To use the application, simply copy the url of any tumblr.com blog you want to backup from into the textbox at the bottom. Afterwards, click on 'Add Blog' on the right of it.
  * Alternatively, if you copy (ctrl-c) a whole _tumblr.com_ blog url from the address bar/text file, the clipboard monitor from TumblThree will detect it and automatically add the blog.
  * To start the download process, click on 'Crawl'. The application will regularly check for (new) blogs in the queue and start processing them, until you stop the application by pressing 'Stop'. So, you can either add blogs to the queue via 'Add to Queue'/double click first and then click 'Crawl', or you start the download process first and add blogs to the queue afterwards.
  * A light blue bar left to the blog in the queue indicates a actively downloaded blog.
  * The blog manager on the left side also indicates the state of each blog. A red background shows an offline blog, a green background an actively crawled blog and a purple background an enqueue blog.
  * You can set up more than one parallel download in the 'Settings'. Also, it is possible to change the download location and the sizes of the picture and video files to download. It is possible to setup a timer for automatic start of the download. 
  * In the Details window you can view statistics of your blog and set blog specific options. You can choose here what kind of post type (photo, video, audio, text, conversation, quote, link) to download.
  * For downloading private blogs (login required blogs), you have to do some steps:
    1. Go to Settings, click the Authenticate button. Logon to tumblr using an account. The window/browser should automatically close after the login indicating a successful authentication. TumblThree will use the Internet Explorer cookies for authentication. Alternatively, you can also use the Internet Explorer directly for logging in to the Tumblr.com network.
    2. Add the blog url.
  * For downloading liked photos and videos, you have to do some steps:
    1. Go to Settings, click the Authenticate button. Logon to tumblr using an account. The window/browser should automatically close after the login indicating a successful authentication. TumblThree will use the Internet Explorer cookies for authentication. Alternatively, you can also use the Internet Explorer directly for logging in to the Tumblr.com network.
    2. Add the blog url including the liked/by string in the url (e.g. https://www.tumblr.com/liked/by/wallpaperfx/).

* Tags

  * You can also download only tagged images by adding tags in a comma separated list in the tag column of the blog list in the top. For example: _great big car,bears_ would search for images that are tagged for either a _great big car_ or _bears_ or both.

* Key Mappings

  * double click on a blog adds it to the queue
  * drag and drop of blogs from the manager (left side) to the queue.
  * space -- start crawl
  * ctrl-space -- pause crawl
  * shift-space -- stop crawl
  * del -- remove blog from queuelist
  * shift-del -- remove blog from blogmanager.
  * ctrl-shift-g -- manually trigger the garbage collection
	
* Saved Settings

  * Application settings are stored in _C:\\Users\\Username\\AppData\\Local\\TumblThree\\_. 
  * You can use the _portable mode_ (settings->general) to stores the application settings in the same folder as the executable.
  * For each blog there is also an index file in the download location (default: in the _.\\Blogs\\_ folder relative to the executable) named after the _blogname_.tumblr. Here are blog relative information stored like what files have been downloaded, the url of the blog and when it was added. This allows you to move your downloaded files (photos, videos, audio files) to a different location without interfering with the backup process.

### Current Limitations: ###

* The old datasets from TumblTwo and TumblOne are NOT compatible yet.
* No more support for Windows XP.
 
### How To Build The Source Code To Help Further Developing: ###

* Download [Visual Studio](https://www.visualstudio.com/vs/community/). The minimum required version is Visual Studio 2015 (C# 6.0 feature support). You also need _Blend for Visual Studio SDK for .NET_ which includes the System.Windows.Interactivity and Microsoft.Expression.Interactions dlls.
* Download the [source code as .zip file](https://github.com/johanneszab/TumblThree/archive/master.zip) or use the [GitHub Desktop](https://desktop.github.com/) and [checkout the code](https://github.com/johanneszab/TumblThree.git).
* Open the TumblThree.sln solution file in the src/ directory of the code.
* Build the Source once before editing anything. Build->Build Solution.

### Translations wanted: ###

* If you want to help translate TumblThree, there are two resource files (.resx) which contain all the strings used in the application. One for [the user interface](https://github.com/johanneszab/TumblThree/blob/master/src/TumblThree/TumblThree.Presentation/Properties/Resources.resx#L120) and one for the [underlying application](https://github.com/johanneszab/TumblThree/blob/master/src/TumblThree/TumblThree.Applications/Properties/Resources.resx#L120).  
* Translate all the words or its meanings between the two value tags and create a pull request on github or simply send me the files via email.
 
### New Feature Request: ###

* [See the Wiki page for ideas of new or missing features](https://github.com/johanneszab/TumblThree/wiki/New-Feature-Requests-and-Possible-Enhancements) and add your thoughts.
