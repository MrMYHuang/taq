Read Me
==========
This document is very incomplete. The author will complete it at his free time.

Download Built App
==========
TAQ app has been built and published to Windows Store. It can be download here:
https://www.microsoft.com/store/apps/9nblggh43pdr
The built app basically supports these Windows 10 devices: Windows 10 Desktop, Windows 10 Mobile, Xbox One, and Holographic devices (e.g. HoloLens). However, the main tested devices are Windows 10 Desktop.

Requirements
=========
The following lists requirements to run all functions of TAQ app:

* Visual Studio 2017 or above is strongly recommanded.
* Windows 10 SDK 10.0.14393.0 is recommanded.
* Syncfusion Essential Studio for Universal Windows. The version 14.3.0.52 is recommanded.
* HockeyApp ID.
* Bing Maps service token.
* Auth0 domain and client ID.
* TAQ server app: https://github.com/MrMYHuang/taqServ

Quick Start
==========
git clone taq
Generate assets:
taq.png

What's Template Files?
==========
Notice, you can not build TAQ app immediately after you clone this repo, because the author removes sensitive information, e.g., Bing Map service token, out of this repo by *template* files. These template files come from removing sensitive information of original source files and then appending ".template" to them. For example, an original source file, Params.cs, has a variable bingMapToken, which stores a Bing Map token used by AqSiteMap.xaml.cs. However, Params.cs is not tracked by the Git repo. Instead, Params.cs is copied to Params.cs.template and sensitive information removed. This template file is the real file tracked by the Git repo. Thus, you can safely modify the original source, Params.cs, without accidentally commiting sensitive information to the Git repo. If you want to add new variables to the parameter settings, you shall add them to both the original source file and the template file, e.g., Params.cs and Params.cs.template. It seems to be troublesome that you have to maintain two versions of parameter settings. Actually, it is more convenient than you track / untrack only the original source files, e.g., Params.cs. If you tracked only the original source files, you would have to untrack them before you add sensitive information to them. And if you added new variables to them, you would have to remove all sensitive information from them before commiting them to the repo! Very troublesome process.
