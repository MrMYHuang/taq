﻿using System.Text.RegularExpressions;
using Windows.Security.Authentication.Web;

namespace Taq.Shared.Models
{
    public static class Params
    {
        // The AQ data filename.
        public const string aqDbFile = "taqi.json";
        // TAQ server URI.
#if DEBUG
        public static string uriHost = "http://localhost:1337/";
#else
        public static string uriHost = "https://YourTaqServerIp/";
#endif
        // Table name of AQ histories.
        public static string aqHistTabName = "epatw";
        // Download file name of AQ history.
        public static string aqHistFile = "AqHist.json";
        public static string hockeyAppId = "YourHockeyAppId";
        public static string bingMapToken = "YourBingMapToken";
		// facebook app ID.
        public static string auth0Domain = "YourAuth0Domain";
        public static string auth0ClientId = "YourAuth0ClientId";

    }
}
