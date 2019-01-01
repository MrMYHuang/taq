Taiwan Air Quality (TAQ) Client App for Universal Windows Platform (UWP)
----------

Read Me
==========
Taiwan Air Quality (TAQ) client app fetches air quality data from TAQ server and presents them in several ways, e.g., info grids, map icons, lists to user. This document is very incomplete. The author will complete it at his free time.

Download Built App
==========
TAQ app has been built and published to Windows Store. It can be download here:
https://www.microsoft.com/store/apps/9nblggh43pdr
The built app basically supports these Windows 10 devices: Windows 10 Desktop, Windows 10 Mobile, Xbox One, and Holographic devices (e.g. HoloLens). However, the main tested devices are Windows 10 Desktop.

Requirements
=========
The following lists requirements to build and run all functions of TAQ app:

* Visual Studio 2017 or above is strongly recommanded.
* Windows 10 SDK 10.0.14393.0 or above is recommanded.
* Essential Studio Enterprise Edition. Get a Community license for UWP from: https://www.syncfusion.com/
* HockeyApp ID. Free sign up at: http://hockeyapp.net
* Bing Maps service token. Free acquire key at: https://www.bingmapsportal.com/
* Auth0 domain and client ID. Free sign up at: http://auth0.com
* TAQ server app: https://github.com/MrMYHuang/taqServ

Quick Start
==========
git clone taq
Generate assets:
taq.png

Notices
==========
The author removes sensitive information, e.g., Bing Map service token, out of these files Taq.Shared/Models/Params.cs and Taq.Uwp/Taq.Uwp.csproj. Please modify these files before buildind and running this app.