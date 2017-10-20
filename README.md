# TumblThree - A Tumblr Blog Backup Application

TumblThree is the code rewrite of [TumblTwo](https://github.com/johanneszab/TumblTwo), a free and open source Tumblr blog backup application, using C# with WPF and the MVVM pattern. It uses the [Win Application Framework (WAF)](https://github.com/jbe2277/waf). It downloads photo, video, audio and text posts from a given tumblr blog.

_Read this in other languages: [简体中文](https://github.com/Emphasia/TumblThree-zh)._

## Features:

* Source code at github (Written in C# using WPF and MVVM).
* Multiple concurrent downloads of a single blog.
* Multiple concurrent downloads of different blogs.
* Internationalization support (currently available: zh, ru, de, fr, es).
* A download queue.
* Autosave of the queuelist.
* Save, clear and restore the queuelist.
* A clipboard monitor that detects *http(s):// .tumblr.com* urls in the clipboard (copy and paste) and automatically adds the blog to the bloglist.
* A settings panel (change download location, turn preview off/on, define number of concurrent downloads, set the imagesize of downloaded pictures, set download defaults, enable portable mode, etc.).
* Allows to set a proxy.
* A bandwidth throttler.
* An option to download an url list instead of the actual files.
* Set a start time for a automatic download (e.g. during nights).
* Uses SSL instead of unsecure http connections.
* Preview of photos & videos.
* Taskbar buttons and key bindings.

### Blog backup/download:

* Download of photo, video (only tumblr.com hosted), text, audio, quote, conversation, link and question posts.
* Download meta information for photo, video and audio posts.
* Downloads inlined photos and videos (e.g. photos embedded in question&answer posts).
* Download of \_raw image files (original/higher resolution pictures). 
* Download of safe mode blogs.
* Allows to download only original content of the blog and skip reblogged posts.
* Can download only tagged posts.
* Can download only specific blog pages instead of the whole blog.
* Allows to download blog posts in a defined time span.
* Can download hidden blogs (login required / dash board blogs).
* Can download password protected blogs (of non-hidden blogs).

### Liked/by backup/download:

* A downloader for downloading "liked by" photos and videos instead of a tumblr blog (e.g. https://www.tumblr.com/liked/by/wallpaperfx/) (login required).
* Download of \_raw image files (original/higher resolution pictures). 

### Tumblr search backup/download:

* A downloader for downloading photos and videos from the tumblr search (e.g. http://www.tumblr.com/search/my+keywords).
* Download of \_raw image files (original/higher resolution pictures). 

### Tumblr tag search backup/download:

* A downloader for downloading photos and videos from the tumblr tag search (e.g. http://www.tumblr.com/tagged/my+keywords) (login required).
* Download of \_raw image files (original/higher resolution pictures). 
* Allows to download posts in a defined time span.

## Download:

Latest releases can be found [here](https://github.com/johanneszab/TumblThree/releases).

## Screenshot:
![TumblThree Main UI](http://www.jzab.de/sites/default/files/images/tumblthree.png?raw=true "TumblThree Main UI")

## Application Usage:

* extract the .zip file and run the application by double clicking TumblThree.exe. The application now comes as a zip file as some parts of it are modular .dll files like internationalization support.
* To use the application, simply copy the url of any tumblr.com blog you want to backup from into the textbox at the bottom. Afterwards, click on 'Add Blog' on the right of it.
* Alternatively, if you copy (ctrl-c) a whole _tumblr.com_ blog url from the address bar/text file, the clipboard monitor from TumblThree will detect it and automatically add the blog.
* To start the download process, click on 'Crawl'. The application will regularly check for (new) blogs in the queue and start processing them, until you stop the application by pressing 'Stop'. So, you can either add blogs to the queue via 'Add to Queue'/double click first and then click 'Crawl', or you start the download process first and add blogs to the queue afterwards.
* A light blue bar left to the blog in the queue indicates a actively downloaded blog.
* The blog manager on the left side also indicates the state of each blog. A red background shows an offline blog, a green background an actively crawled blog and a purple background an enqueue blog.
* You can set up more than one parallel download in the 'Settings'. Also, it is possible to change the download location and the sizes of the picture and video files to download. It is possible to setup a timer for automatic start of the download. 
* In the Details window you can view statistics of your blog and set blog specific options. You can choose here what kind of post type (photo, video, audio, text, conversation, quote, link) to download.
* For downloading only tagged posts, you'll have to do some steps:
  1. Add the blog url.
  2. Open the blog in the details tab, enter the tags in the Tags textbox in a comma separated list without the leading hash (#) sign. E.g. _great big car,bears_ would search for images that are tagged for either a _great big car_ or _bears_ or both.
* For downloading password protected blogs, you'll have to do some steps:
  1. Add the blog url.
  2. Open the blog in the details tab, enter the password in the Password textbox.
* For downloading hidden blogs (login required blogs), you have to do some steps:
  1. Go to Settings, click the Authenticate button. Logon to tumblr using an account. The window/browser should automatically close after the login indicating a successful authentication. TumblThree will use the Internet Explorer cookies for authentication. Alternatively, you can also use the Internet Explorer directly for logging in to the Tumblr.com network.
  2. Add the blog url.
* For downloading liked photos and videos, you'll have to do some steps:
  1. Go to Settings, click the Authenticate button. Logon to tumblr using an account. The window/browser should automatically close after the login indicating a successful authentication. TumblThree will use the Internet Explorer cookies for authentication. Alternatively, you can also use the Internet Explorer directly for logging in to the Tumblr.com network.
  2. Add the blog url including the liked/by string in the url (e.g. https://www.tumblr.com/liked/by/wallpaperfx/).
* For downloading photos and videos from the tumblr tag search, you'll have to do some steps:
  1. Go to Settings, click the Authenticate button. Logon to tumblr using an account. The window/browser should automatically close after the login indicating a successful authentication. TumblThree will use the Internet Explorer cookies for authentication. Alternatively, you can also use the Internet Explorer directly for logging in to the Tumblr.com network.
  2. Add the search url including your tags separated by plus signs (+) in the url (e.g. https://www.tumblr.com/tagged/my+special+tags).
* Key Mappings:
  * double click on a blog adds it to the queue
  * drag and drop of blogs from the manager (left side) to the queue.
  * space -- start crawl
  * ctrl-space -- pause crawl
  * shift-space -- stop crawl
  * del -- remove blog from queuelist
  * shift-del -- remove blog from blogmanager.
  * ctrl-shift-g -- manually trigger the garbage collection

## Getting Started:

The default settings should cover most users. You should only have to change the download location and the kind of posts you want to download. For this, in the Settings (click on the Settings button in the lower panel of the main user interface) you might want to change:
* General -> Download location: Specifies where to download the files. The default is in a folder _Blogs_ relative to the TumblThree.exe
* Blog -> Settings applied to each blog upon addition:
  * Here you can set what posts newly added blogs will download per default. To change what each blog downloads, click on a blog in the main interface, select the Details Tab on the right and change the settings. This separation allows to download different kind of post for different blogs. You can change the download settings for multiple existing blogs by selecting them with shift+left click for a range or ctrl-a for all of them.
  * Note: You might want to always select:
    *  _Download Reblogged posts_: Downloads reblogs, not just original content of the blog author.
    *  _Force Rescan_: Force Rescan always crawls the whole blog and not just new posts which were added after the last successful crawl. The statistics of a blog (total posts, number of post, number of duplicates) currently can only be updated if the whole blog is crawled. Thus, disabling this might result in downloading "more" posts than displayed in TumblThree. If you don't matter if about the displayed blog statistics, turning Force Rescan off will decrease the scanning time since already downloaded posts are skipped in the scanning.

Settings you might want to change if the download speed is not satisfactory:
* General -> Parallel connections: Specifies the number of connections used for downloading posts. The number is shared between all actively downloading blogs. __Note:__ If you only download video blogs, you might want to decrease the number of parallel connections to eight or below, depending on your internet connection speed. Otherwise you might end up with incomplete files as the tumblr servers regularly close all open connections if there are too many open. For small files (images) this generally isn't an issue.
* General -> Parallel Blogs: Number of blogs to download in parallel.

Most likely you don't have to change any of the other connection settings. In particular, settings you should never change, unless you're sure you know what you are doing:
* General -> Limit Tumblr Api Connections: Leave this checkbox checked and do not change the corresponding values of 90 connections per 60 seconds. If you still change them, you might end up with offline blogs or missing downloads.

## Further Insights:

* _Note:_ All the follwing files are stored in json format and can be opened in any editor.
* Application settings are stored in _C:\\Users\\Username\\AppData\\Local\\TumblThree\\_. 
* You can use the _portable mode_ (settings->general) to stores the application settings in the same folder as the executable.
* For each blog there is also a database (serialied class) file in the _Index_ folder of the download location named after the _blogname_.tumblr. Here are blog relative information stored like what files have been downloaded, the url of the blog and when it was added. This allows you to move your downloaded files (photos, videos, audio files) to a different location without interfering with the download process.
* Some settings aren't hooked up to the graphical user interfacae. It's possible to view all TumblThree settings by opening the settings.json in any editor located in _C:\\Users\\Username\\AppData\\Local\\TumblThree\\_. Their names should be self explainatory. Some notable settings to further fine tune the application include:
  * BufferSize: Allows to set the buffer size for downloading binary files (photos, videos) in multiples of 4KB. The default is 2MB, thus the BufferSize has a value of 512. Increasing this value reduces disk fragmentation as more of the file is kept in the memory before it gets written out to the disk but increases the memory usage.
  * MaxNumberOfRetries: Sets the maximum number of retries if a tumblr server forcefully closes the connection. This might regularily happen on the tumblr video host (vt.tumblr.com) if too many connections were opened in parallel. After the limit is exhausted, the file is left truncated, but is also not registered as a successful downloaded. Thus, the file can be resumed in the next crawl.
  * TumblrHosts: Contains a list of hosts which is tried for downloading \_raw photos if the photo size is set to _raw_. If none of the hosts contains the \_raw version, the actually scanned host is tried with the next lower resolution (1028). 

## Limitations:

* The old datasets from TumblTwo and TumblOne are __not__ compatible.
* No more support for Windows XP.
 
## How To Build The Source Code To Help Further Developing:

* Download [Visual Studio](https://www.visualstudio.com/vs/community/). The minimum required version is Visual Studio 2015 (C# 6.0 feature support). You also need _Blend for Visual Studio SDK for .NET_ which includes the System.Windows.Interactivity and Microsoft.Expression.Interactions dlls.
* Download the [source code as .zip file](https://github.com/johanneszab/TumblThree/archive/master.zip) or use the [GitHub Desktop](https://desktop.github.com/) and [checkout the code](https://github.com/johanneszab/TumblThree.git).
* Open the TumblThree.sln solution file in the src/ directory of the code.
* Build the Source once before editing anything. Build->Build Solution.

## Translations wanted:

* If you want to help translate TumblThree, there are two resource files (.resx) which contain all the strings used in the application. One for [the user interface](https://github.com/johanneszab/TumblThree/blob/master/src/TumblThree/TumblThree.Presentation/Properties/Resources.resx#L120) and one for the [underlying application](https://github.com/johanneszab/TumblThree/blob/master/src/TumblThree/TumblThree.Applications/Properties/Resources.resx#L120).  
* Translate all the words or its meanings between the two value tags and create a pull request on github or simply send me the files via email.
 
## New Feature Requests:

* [See the Wiki page for ideas of new or missing features](https://github.com/johanneszab/TumblThree/wiki/New-Feature-Requests-and-Possible-Enhancements) and add your thoughts.
