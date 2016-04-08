# TumblThree - A Tumblr Image Downloader

TumblThree is the code rewrite of [TumblTwo](https://github.com/johanneszab/TumblTwo) using C# with WPF and the MVVM pattern. It uses the [WPF Application Framework (WAF)](https://waf.codeplex.com/).

### New Features (over TumblTwo):
* Internationalization support.
* Autosave of the queuelist.
* Save, clear and restore the queuelist.
* Taskbar buttons and key bindings.

### New Features (over TumblOne):
* Multiple simultaneous picture downloads of a single blog.
* Multiple simultaneous downloads of different blogs, customizable in the settings.
* It is possible to download images from blogs only for specific tags.
* A clipboard monitor that detects *http(s):// .tumblr.com* urls in the clipboard (copy and paste) and automatically adds the blog to the bloglist.
* A download queue for blogs.
* A detection if the blog is still online or the owner has changed.
* The blogview is now sortable and shows more information, e.g. date added, last time finished and the progress.
* A settings panel (change download location, turn picture preview off/on, define number of simultaneous downloads, set the imagesize of downloaded pictures).
* Source code at github (Written in C# using WPF and MVVM).

Current binaries and more frequently updated information can also be found at [the project homepage](http://www.jzab.de/content/tumblthree).