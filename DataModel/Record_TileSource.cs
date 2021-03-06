﻿using LolloGPS.Data.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using Utilz;

namespace LolloGPS.Data
{
    [DataContract]
    public sealed class TileSourceRecord : ObservableData
    {
        private const string DefaultTileSourceTechName = "Nokia";
        private const string AllTileSourceTechName = "All";
        private const string NoTileSourceTechName = "None";
        private const string SampleTileSourceTechName = "YourOwnTileSource"; 

        private const string DefaultTileSourceDisplayName = "Built in";
        private const string AllTileSourceDisplayName = "All";
        private const string NoTileSourceDisplayName = "None";

        private static readonly string DefaultTileSourceUriString = string.Empty;
        private static readonly string DefaultTileSourceProviderUriString = string.Empty;
        private static readonly string DummyTileSourceUriString = string.Empty;
        private static readonly string DummyTileSourceProviderUriString = string.Empty;
        private static readonly string SampleUriString = "http://tileserver.something/" + ZoomLevelPlaceholder + "/" + XPlaceholder + "/" + YPlaceholder + ".png";

        private const int DefaultTileSourceMinZoom = 0;
        private const int DummyTileSourceMinZoom = 0;

        private const int DefaultTileSourceMaxZoom = 20;
        private const int DummyTileSourceMaxZoom = 0;

        private const int DefaultTileSourceTilePixelSize = 256;        
        private const int DummyTileSourceTilePixelSize = 0;

        public const string ZoomLevelPlaceholder = "{zoomlevel}";
        public const string XPlaceholder = "{x}";
        public const string YPlaceholder = "{y}";
        public const string ZoomLevelPlaceholder_Internal = "{0}";
        public const string XPlaceholder_Internal = "{1}";
        public const string YPlaceholder_Internal = "{2}";

        public const int DefaultMinZoom = 0;
        public const int DefaultMaxZoom = 16;
        public const int DefaultTilePixelSize = 256;
        public const int DefaultMinTilePixelSize = 64;
        public const int DefaultMaxTilePixelSize = 1024;
        public const int MaxTechNameLength = 25;

        private string _techName = TileSourceRecord.DefaultTileSourceTechName;
        [DataMember]
        public string TechName { get { return _techName; } set{ _techName = value; RaisePropertyChanged(); } }

        private string _displayName = TileSourceRecord.DefaultTileSourceDisplayName;
        [DataMember]
        public string DisplayName { get { return _displayName; } set{ _displayName = value; RaisePropertyChanged(); } }

        private string _uriString = SampleUriString;
        [DataMember]
        public string UriString { get { return _uriString; } set{ _uriString = value; RaisePropertyChanged(); } }

        private string _providerUriString = string.Empty;
        [DataMember]
        public string ProviderUriString { get { return _providerUriString; } set { _providerUriString = value; RaisePropertyChanged(); } }

        private int _minZoom = DefaultMinZoom;
        [DataMember]
        public int MinZoom { get { return _minZoom; } set{ _minZoom = value; RaisePropertyChanged(); } }

        private int _maxZoom = DefaultMaxZoom;
        [DataMember]
        public int MaxZoom { get { return _maxZoom; } set{ _maxZoom = value; RaisePropertyChanged(); } }

        private int _tilePixelSize = DefaultTilePixelSize;
        [DataMember]
        public int TilePixelSize { get { return _tilePixelSize; } set{ _tilePixelSize = value; RaisePropertyChanged(); } }

        //private bool _isTesting = false;
        //[DataMember]
        //public bool IsTesting { get { return _isTesting; } set { _isTesting = value; RaisePropertyChanged(); } }

        private bool _isDeletable = false;
        [DataMember]
        public bool IsDeletable { get { return _isDeletable; } set{ _isDeletable = value; RaisePropertyChanged(); } }

        //private bool _isValid = false;
        //[DataMember]
        //public bool IsValid { get { return _isValid; } set { _isValid = value; RaisePropertyChanged(); } }

        [IgnoreDataMember]
        public bool IsDefault { get { return _techName == TileSourceRecord.DefaultTileSourceTechName; } }
        [IgnoreDataMember]
        public bool IsAll { get { return _techName == TileSourceRecord.AllTileSourceTechName; } }
        [IgnoreDataMember]
        public bool IsNone { get { return _techName == TileSourceRecord.NoTileSourceTechName; } }
        [IgnoreDataMember]
        public bool IsSample { get { return _techName == TileSourceRecord.SampleTileSourceTechName; } }

        [IgnoreDataMember]
        public int MaxTechNameLengthProp { get { return MaxTechNameLength; } }

        public TileSourceRecord(string techName, string displayName, string uri, string providerUri, int minZoom, int maxZoom, int tilePixelSize, bool isDeletable) //, bool isValid)
        {
            TechName = techName;
            DisplayName = displayName;
            UriString = uri;
            ProviderUriString = providerUri;
            MinZoom = minZoom;
            MaxZoom = maxZoom;
            TilePixelSize = tilePixelSize;
            //IsTesting = isTesting;
            IsDeletable = isDeletable;
            //IsValid = isValid;
        }
        public string Check(bool skipAlreadyInUse) //string techName, string displayName, string uri, int minZoom, int maxZoom, int tilePixelSize, bool isTesting)
        {
            //IsValid = false;

            string errorMsg = string.Empty;
            errorMsg = CheckTechName(TechName, skipAlreadyInUse);
            if (!string.IsNullOrEmpty(errorMsg)) return errorMsg;
            errorMsg = CheckDisplayName(DisplayName, skipAlreadyInUse); // not sure we want to ask for a display name
            if (!string.IsNullOrEmpty(errorMsg)) return errorMsg;
            errorMsg = CheckUri(UriString, false);
            if (!string.IsNullOrEmpty(errorMsg)) return errorMsg;
            errorMsg = CheckUri(ProviderUriString, true);
            if (!string.IsNullOrEmpty(errorMsg)) return errorMsg;
            errorMsg = CheckMinMaxZoom(MinZoom, MaxZoom);
            if (!string.IsNullOrEmpty(errorMsg)) return errorMsg;
            errorMsg = CheckTilePixelSize(TilePixelSize);
            if (!string.IsNullOrEmpty(errorMsg)) return errorMsg;

            //IsValid = true;
            return string.Empty;
        }
        private static string CheckTechName(string techName, bool skipAlreadyInUse)
        {
            if (string.IsNullOrWhiteSpace(techName)) return "Name must not be empty";
            if (techName.Length > MaxTechNameLength) return string.Format("Name must be max {0} characters", MaxTechNameLength);
            //int invalidCharIndex = techName.IndexOfAny(Path.GetInvalidPathChars());
            //if (invalidCharIndex >= 0) return string.Format("{0} is an invalid character", techName.Substring(invalidCharIndex, 1));
            //invalidCharIndex = techName.IndexOfAny(Path.GetInvalidFileNameChars());
            //if (invalidCharIndex >= 0) return string.Format("{0} is an invalid character", techName.Substring(invalidCharIndex, 1));
            if (!techName.All(Char.IsLetter)) return "Name: only letters are allowed";
            if (!skipAlreadyInUse)
            {
                if (PersistentData.GetInstance().TileSourcez.FirstOrDefault(a => a.TechName == techName) != null) return "Name already in use, use a new name to make changes";
                if (PersistentData.GetInstance().TileSourcez.FirstOrDefault(a => a.DisplayName == techName) != null) return "Name already in use, use a new name to make changes";
            }

            return string.Empty;
        }
        private static string CheckDisplayName(string displayName, bool skipAlreadyInUse)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return "Name must not be empty";
            if (displayName.Length > MaxTechNameLength) return string.Format("Name must be max {0} characters", MaxTechNameLength);
            if (!skipAlreadyInUse)
            {
                if (PersistentData.GetInstance().TileSourcez.FirstOrDefault(a => a.TechName == displayName) != null) return "Name already in use, use a new name to make changes";
                if (PersistentData.GetInstance().TileSourcez.FirstOrDefault(a => a.DisplayName == displayName) != null) return "Name already in use, use a new name to make changes";
            }

            return string.Empty;
        }
        private static string CheckUri(string uri, bool isEmptyAllowed)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                if (isEmptyAllowed) return string.Empty;
                else return "Uri is empty";
            }
            if (uri.Length < 7) return "Invalid uri";
            if (!uri.Contains(ZoomLevelPlaceholder)) return string.Format("Uri must contain {0}", ZoomLevelPlaceholder);
            if (!uri.Contains(XPlaceholder)) return string.Format("Uri must contain {0}", XPlaceholder);
            if (!uri.Contains(YPlaceholder)) return string.Format("Uri must contain {0}", YPlaceholder);
            if (uri.Count(u => u == '{') > 3) return "Uri may only have three placeholders";
            if (uri.Count(u => u == '}') > 3) return "Uri may only have three placeholders";

            // check if valid uri
            string sTestUri = string.Empty;
            try
            {
                string testUriFormat = uri.Replace(TileSourceRecord.ZoomLevelPlaceholder, TileSourceRecord.ZoomLevelPlaceholder_Internal);
                testUriFormat = testUriFormat.Replace(TileSourceRecord.XPlaceholder, TileSourceRecord.XPlaceholder_Internal);
                testUriFormat = testUriFormat.Replace(TileSourceRecord.YPlaceholder, TileSourceRecord.YPlaceholder_Internal);

                sTestUri = string.Format(testUriFormat, 0, 0, 0);

                bool isWellFormed = Uri.IsWellFormedUriString(sTestUri, UriKind.Absolute);
                if (!isWellFormed) return "Uri format is invalid";

                var builder = new UriBuilder(sTestUri);
            }
            catch (Exception)
            {
                return "Uri format is invalid";
            }
            if (RuntimeData.GetInstance().IsConnectionAvailable)
            {
                try
                {
                    var request = WebRequest.CreateHttp(sTestUri);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
//            else // LOLLO TODO do I need this? If so, put it in version 10
//            {
//                return "Connection not available or metered connection barred";
//            }
            return string.Empty;
        }
        public static string CheckMinMaxZoom(int minZoom, int maxZoom)
        {
            if (minZoom < DefaultTileSourceMinZoom) return string.Format("Min zoom must be at least {0}", DefaultTileSourceMinZoom);
            if (minZoom > DefaultTileSourceMaxZoom) return string.Format("Min zoom must not exceed {0}", DefaultTileSourceMaxZoom);
            if (maxZoom < DefaultTileSourceMinZoom) return string.Format("Max zoom must be at least {0}", DefaultTileSourceMinZoom);
            if (maxZoom > DefaultTileSourceMaxZoom) return string.Format("Max zoom must not exceed {0}", DefaultTileSourceMaxZoom);
            if (minZoom > maxZoom) return "Min zoom must not exceed max zoom";
            return string.Empty;
        }
        private static string CheckTilePixelSize(int tilePixelSize)
        {
            if (tilePixelSize < DefaultMinTilePixelSize) return string.Format("Tile size must be at least {0}", DefaultMinTilePixelSize);
            if (tilePixelSize > DefaultMaxTilePixelSize) return string.Format("Tile size must not exceed {0}", DefaultMaxTilePixelSize);
            return string.Empty;
        }
        public static void Clone(TileSourceRecord source, ref TileSourceRecord target)
        {
            if (source != null)
            {
                if (target == null) target = GetDefaultTileSource();

                target.TechName = source.TechName;
                target.DisplayName = source.DisplayName;
                target.UriString = source.UriString;
                target.ProviderUriString = source.ProviderUriString;
                target.MinZoom = source.MinZoom;
                target.MaxZoom = source.MaxZoom;
                target.TilePixelSize = source.TilePixelSize;
                //target.IsTesting = source.IsTesting;
                target.IsDeletable = source.IsDeletable;
                //target.IsValid = source.IsValid;
            }
        }
        public bool IsEqualTo(TileSourceRecord comp)
        {
            if (comp == null) return false;

            return comp.TechName == TechName
                && comp.DisplayName == DisplayName
                && comp.UriString == UriString
                && comp.ProviderUriString == ProviderUriString
                && comp.MinZoom == MinZoom
                && comp.MaxZoom == MaxZoom
                && comp.TilePixelSize == TilePixelSize
                //&& comp.IsTesting == IsTesting
                && comp.IsDeletable == IsDeletable;
        }
        public static List<TileSourceRecord> GetDefaultTileSources()
        {
            List<TileSourceRecord> output = new List<TileSourceRecord>();
            output.Add(new TileSourceRecord(DefaultTileSourceTechName, DefaultTileSourceDisplayName, DefaultTileSourceUriString, DefaultTileSourceProviderUriString, DefaultTileSourceMinZoom, DefaultTileSourceMaxZoom, DefaultTileSourceTilePixelSize, false));

            output.Add(new TileSourceRecord("ForUMaps", "4UMaps", "http://4umaps.eu/{zoomlevel}/{x}/{y}.png", "http://www.4umaps.eu/", 2, 15, 256, false));

            output.Add(new TileSourceRecord("OpenTopoMap", "OpenTopoMap", "http://server.opentopomap.org/{zoomlevel}/{x}/{y}.png", "http://opentopomap.org/", 2, 15, 256, false));
            output.Add(new TileSourceRecord("OpenStreetMap", "OpenStreetMap", "http://a.tile.openstreetmap.org/{zoomlevel}/{x}/{y}.png", "http://www.openstreetmap.org/", 0, 18, 256, false));
            output.Add(new TileSourceRecord("OpenStreetMapBW", "OpenStreetMap BW", "http://a.www.toolserver.org/tiles/bw-mapnik/{zoomlevel}/{x}/{y}.png", "http://www.openstreetmap.org/", 0, 18, 256, false));
            output.Add(new TileSourceRecord("OpenBusMap", "OpenBusMap", "http://tileserver.memomaps.de/tilegen/{zoomlevel}/{x}/{y}.png", "http://openbusmap.org/", 3, 18, 256, false));
            output.Add(new TileSourceRecord("OpenSeaMap", "OpenSeaMap", "http://tiles.openseamap.org/seamark/{zoomlevel}/{x}/{y}.png", "http://openseamap.org/?L=1", 9, 18, 256, false));
            output.Add(new TileSourceRecord("MapQuestOSM", "MapQuest OSM", "http://otile1.mqcdn.com/tiles/1.0.0/osm/{zoomlevel}/{x}/{y}.png", "http://www.mapquest.com/", 0, 18, 256, false));
            output.Add(new TileSourceRecord("HikeBike", "Hike & Bike Map", "http://a.tiles.wmflabs.org/hikebike/{zoomlevel}/{x}/{y}.png", "http://hikebikemap.org/", 0, 16, 256, false));
#if NOSTORE
            output.Add(new TileSourceRecord("ArcGIS", "ArcGIS", "http://services.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{zoomlevel}/{y}/{x}", DefaultTileSourceProviderUriString, 0, 16, 256, false));
#endif
            output.Add(new TileSourceRecord("ThunderforestLandscape", "Thunderforest Landscape", "http://a.tile.thunderforest.com/landscape/{zoomlevel}/{x}/{y}.png", "http://www.thunderforest.com/", 2, 18, 256, false));
            output.Add(new TileSourceRecord("ThunderforestCycle", "Thunderforest Cycle", "http://a.tile.thunderforest.com/cycle/{zoomlevel}/{x}/{y}.png", "http://www.thunderforest.com/", 2, 18, 256, false));
            output.Add(new TileSourceRecord("ThunderforestOutdoors", "Thunderforest Outdoors", "http://a.tile.thunderforest.com/outdoors/{zoomlevel}/{x}/{y}.png", "http://www.thunderforest.com/", 2, 18, 256, false));
            output.Add(new TileSourceRecord("ThunderforestTransport", "Thunderforest Transport", "http://a.tile.thunderforest.com/transport/{zoomlevel}/{x}/{y}.png", "http://www.thunderforest.com/", 2, 18, 256, false));

            output.Add(new TileSourceRecord("StamenToner", "Stamen Toner", "http://tile.stamen.com/toner/{zoomlevel}/{x}/{y}.png", "http://maps.stamen.com/", 2, 18, 256, false));
            output.Add(new TileSourceRecord("StamenTerrain", "Stamen Terrain (N America)", "http://tile.stamen.com/terrain/{zoomlevel}/{x}/{y}.jpg", "http://maps.stamen.com/", 5, 18, 256, false));

            output.Add(new TileSourceRecord("KartatKapsiFiTerrain", "Kartat Kapsi Terrain (FI)", "http://tiles.kartat.kapsi.fi/peruskartta/{zoomlevel}/{x}/{y}.jpg", "http://kartat.kapsi.fi/", 2, 17, 256, false));
            output.Add(new TileSourceRecord("KartatKapsiFiBackground", "Kartat Kapsi Background (FI)", "http://tiles.kartat.kapsi.fi/taustakartta/{zoomlevel}/{x}/{y}.jpg", "http://kartat.kapsi.fi/", 2, 17, 256, false));

            output.Add(new TileSourceRecord("OrdnanceSurvey", "Ordnance Survey (UK)", "http://a.os.openstreetmap.org/sv/{zoomlevel}/{x}/{y}.png", "http://www.ordnancesurvey.co.uk/opendata/viewer/index.html", 7, 17, 256, false));
#if NOSTORE
            output.Add(new TileSourceRecord("Swisstopo", "Swisstopo (CH)", "http://mpa3.mapplus.ch/swisstopo/{zoomlevel}/{x}/{y}.jpg", DefaultTileSourceProviderUriString, 7, 16, 256, false));
#endif
            output.Add(new TileSourceRecord("UTTopoLight", "UT Topo Light (Norway)", "http://a-kartcache.nrk.no/tiles/ut_topo_light/{zoomlevel}/{x}/{y}.jpg", "http://ut.no/", 5, 16, 256, false));
            output.Add(new TileSourceRecord("FreemapSlovakia", "Freemap Slovakia", "http://www.freemap.sk/layers/allinone/?/BN/{zoomlevel}/{x}/{y}.png", "http://www.freemap.sk/", 0, 17, 256, false));
            return output;
        }
        public static TileSourceRecord GetDefaultTileSource()
        {
            return new TileSourceRecord(DefaultTileSourceTechName, DefaultTileSourceDisplayName, DefaultTileSourceUriString, DefaultTileSourceProviderUriString, DefaultTileSourceMinZoom, DefaultTileSourceMaxZoom, DefaultTileSourceTilePixelSize, false); //, false, true);
        }
        public static TileSourceRecord GetAllTileSource()
        {
            return new TileSourceRecord(AllTileSourceTechName, AllTileSourceDisplayName, DummyTileSourceUriString, DummyTileSourceProviderUriString, DummyTileSourceMinZoom, DummyTileSourceMaxZoom, DummyTileSourceTilePixelSize, false); //, false, false);
        }
        public static TileSourceRecord GetNoTileSource()
        {
            return new TileSourceRecord(NoTileSourceTechName, NoTileSourceDisplayName, DummyTileSourceUriString, DummyTileSourceProviderUriString, DummyTileSourceMinZoom, DummyTileSourceMaxZoom, DummyTileSourceTilePixelSize, false); //, false, false);
        }
        public static TileSourceRecord GetSampleTileSource()
        {
            return new TileSourceRecord(SampleTileSourceTechName, SampleTileSourceTechName, SampleUriString, DefaultTileSourceProviderUriString, DefaultMinZoom, DefaultMaxZoom, DefaultTilePixelSize, false); //true, false, false);
        }

    }
}
