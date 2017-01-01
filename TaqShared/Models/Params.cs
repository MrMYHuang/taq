using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaqShared.Models
{
    public static class Params
    {
        // The AQ data filename.
        public const string aqDbFile = "taqi.json";
        // TAQ server URI.
        public static string uriHost = "https://YourTaqServerDomainName/";
        // Table name of AQ histories.
        public static string aqHistTabName = "epatw";
        // Download file name of AQ history.
        public static string aqHistFile = "AqHist.json";
    }
}
