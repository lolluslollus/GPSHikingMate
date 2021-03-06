﻿using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LolloGPS.Data
{
    [DataContract]
    public sealed class PointRecord : ObservableData
    {
        #region properties
        private double _latitude = default(Double);
        private double _longitude = default(Double);
        private double _altitude = default(Double);
        private double _accuracy = default(Double);
        private double _altitudeAccuracy = default(Double);
        private UInt32 _howManySatellites = default(UInt32);
        //private double _positionDilutionOfPrecision = default(Double);
        //private double _horizontalDilutionOfPrecision = default(Double);
        //private double _verticalDilutionOfPrecision = default(Double);
        private string _positionSource = String.Empty;
        private Double _speedInMetreSec = default(Double);
        private string _status = String.Empty;
        private DateTime _timePoint = default(DateTime);
        //private string _gpsName = String.Empty;
        //private string _gpsComment = String.Empty;
        private string _humanDescription = String.Empty;
        private string _hyperLink = null;
        private string _hyperLinkText = string.Empty;

        [PrimaryKey, AutoIncrement] // required by SQLite
        public int Id { get; set; } // required by SQLite

        [DataMember]
        public double Latitude { get { return _latitude; } set { _latitude = value; RaisePropertyChanged(); } }
        [DataMember]
        public double Longitude { get { return _longitude; } set { _longitude = value; RaisePropertyChanged(); } }
        [DataMember]
        public double Altitude { get { return _altitude; } set { _altitude = value; RaisePropertyChanged(); } }
        [DataMember]
        public double Accuracy { get { return _accuracy; } set { _accuracy = value; RaisePropertyChanged(); } }
        [DataMember]
        public double AltitudeAccuracy { get { return _altitudeAccuracy; } set { _altitudeAccuracy = value; RaisePropertyChanged(); } }
        [DataMember]
        public UInt32 HowManySatellites { get { return _howManySatellites; } set { _howManySatellites = value; RaisePropertyChanged(); } }
        //[DataMember]
        //public double PositionDilutionOfPrecision { get { return _positionDilutionOfPrecision; } set { _positionDilutionOfPrecision = value; RaisePropertyChanged("PositionDilutionOfPrecision"); } }
        //[DataMember]
        //public double HorizontalDilutionOfPrecision { get { return _horizontalDilutionOfPrecision; } set { _horizontalDilutionOfPrecision = value; RaisePropertyChanged("HorizontalDilutionOfPrecision"); } }
        //[DataMember]
        //public double VerticalDilutionOfPrecision { get { return _verticalDilutionOfPrecision; } set { _verticalDilutionOfPrecision = value; RaisePropertyChanged("LatitudeVerticalDilutionOfPrecision"); } }
        [DataMember]
        public string PositionSource { get { return _positionSource; } set { _positionSource = value; RaisePropertyChanged(); } }
        [DataMember]
        public double SpeedInMetreSec { get { return _speedInMetreSec; } set { _speedInMetreSec = value; RaisePropertyChanged(); } }
        [DataMember]
        public string Status { get { return _status; } set { _status = value; RaisePropertyChanged(); } }
        [DataMember]
        public DateTime TimePoint { get { return _timePoint; } set { _timePoint = value; RaisePropertyChanged(); } }
        //[DataMember]
        //public string GPSName { get { return _gpsName; } set { _gpsName = value; RaisePropertyChanged(); } }
        //[DataMember]
        //public string GPSComment { get { return _gpsComment; } set { _gpsComment = value; RaisePropertyChanged(); } }
        [DataMember]
        public string HumanDescription { get { return _humanDescription; } set { if (_humanDescription != value) { _humanDescription = value; RaisePropertyChanged(); } } }
        [DataMember]
        public string HyperLink { get { return _hyperLink; } set { _hyperLink = value; RaisePropertyChanged(); } }
        [DataMember]
        public string HyperLinkText { get { return _hyperLinkText; } set { _hyperLinkText = value; RaisePropertyChanged(); } }
        #endregion properties

        public PointRecord() { }
        public static void Clone(PointRecord source, ref PointRecord target)
        {
            if (source != null)
            {
                if (target == null) target = new PointRecord();

                target.Accuracy = source.Accuracy;
                target.Altitude = source.Altitude;
                target.AltitudeAccuracy = source.AltitudeAccuracy;
                target.HowManySatellites = source.HowManySatellites;
                target.Latitude = source.Latitude;
                target.Longitude = source.Longitude;
                target.PositionSource = source.PositionSource;
                target.SpeedInMetreSec = source.SpeedInMetreSec;
                target.Status = source.Status;
                //target.HorizontalDilutionOfPrecision = source.HorizontalDilutionOfPrecision;
                //target.PositionDilutionOfPrecision = source.PositionDilutionOfPrecision;
                //target.VerticalDilutionOfPrecision = source.VerticalDilutionOfPrecision;
                target.TimePoint = source.TimePoint;
                //target.GPSName = source.GPSName;
                //target.GPSComment = source.GPSComment;
                target.HumanDescription = source.HumanDescription;
                target.HyperLink = source.HyperLink;
                target.HyperLinkText = source.HyperLinkText;
            }
        }
        public bool IsEqualTo(PointRecord comp)
        {
            if (comp == null) return false;
            return
                Accuracy == comp.Accuracy &&
                Altitude == comp.Altitude &&
                AltitudeAccuracy == comp.AltitudeAccuracy &&
                HowManySatellites == comp.HowManySatellites &&
                Latitude == comp.Latitude &&
                Longitude == comp.Longitude &&
                PositionSource == comp.PositionSource &&
                SpeedInMetreSec == comp.SpeedInMetreSec &&
                Status == comp.Status &&
                    //HorizontalDilutionOfPrecision == comp.HorizontalDilutionOfPrecision &&
                    //PositionDilutionOfPrecision == comp.PositionDilutionOfPrecision &&
                    //VerticalDilutionOfPrecision == comp.VerticalDilutionOfPrecision &&
                TimePoint == comp.TimePoint &&
                    //GPSName == comp.GPSName &&
                    //GPSComment == comp.GPSComment &&
                HumanDescription == comp.HumanDescription &&
                HyperLink == comp.HyperLink &&
                HyperLinkText == comp.HyperLinkText;
        }
        //public void UpdateHumanDescriptionIfHistoryOrLandmarks_TPL(string newValue)
        //{
        //    PersistentData myData = PersistentData.GetInstance();
        //    if (HumanDescription == newValue || (myData.SelectedSeries != PersistentData.Tables.History && myData.SelectedSeries != PersistentData.Tables.Landmarks)) return;
        //    HumanDescription = newValue;
        //    if (myData.SelectedSeries == PersistentData.Tables.History)
        //    {
        //        Task updateHistory = DBManager.UpdateHistoryAsync(this, false);
        //    }
        //    else
        //    {
        //        Task updateLandmarks = DBManager.UpdateLandmarksAsync(this, false);
        //    }
        //}
        public void UpdateHumanDescription_TPL(string newValue)
        {
            if (HumanDescription == newValue) return;
            HumanDescription = newValue;

            PersistentData myData = PersistentData.GetInstance();
            switch (myData.SelectedSeries)
            {
                case PersistentData.Tables.History:
                    Task updateHistory = DBManager.UpdateHistoryAsync(this, false);
                    break;
                case PersistentData.Tables.Route0:
                    Task updateRoute0 = DBManager.UpdateRoute0Async(this, false);
                    break;
                case PersistentData.Tables.Landmarks:
                    Task updateLandmarks = DBManager.UpdateLandmarksAsync(this, false);
                    break;
                default:
                    break;
            }
        }

        public Boolean IsEmpty()
        {
            return !(
                _latitude != default(Double) 
                || _longitude != default(Double) 
                || _altitude != default(Double) 
                || !String.IsNullOrWhiteSpace(_positionSource));
        }
    }
}
