using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace LolloGPS.Data.Constants
{
    public static class ConstantData
    {
        public const String DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";
        public const string MYMAIL = "lollus@hotmail.co.uk";
        public const string APPNAME = "GPS Hiking Mate for Windows Phone 8.1";
        public const string APPNAME_ALL_IN_ONE = "GPSHikingMate";
        public const string BUY_URI = @"ms-windows-store:navigate?appid=c65094e7-897d-4de5-ac65-e14f05ab90f7"; // this id comes from the dashboard
        public const string RATE_URI = @"ms-windows-store:reviewapp?appid=c65094e7-897d-4de5-ac65-e14f05ab90f7"; // this id comes from the dashboard
        public const string GPX_EXTENSION = ".gpx";
        public const string GET_LOCATION_BACKGROUND_TASK_NAME = "GetLocationBackgroundTask";
        public const string GET_LOCATION_BACKGROUND_TASK_ENTRY_POINT = "BackgroundTasks.GetLocationBackgroundTask";

        public static string AppName { get { return ConstantData.APPNAME; } }
        private static string _version = Package.Current.Id.Version.Major.ToString()
            + "."
            + Package.Current.Id.Version.Minor.ToString()
            + "."
            + Package.Current.Id.Version.Build.ToString()
            + "."
            + Package.Current.Id.Version.Revision.ToString();
        public static string Version { get { return "Version " + _version; } }

    }
}
