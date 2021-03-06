﻿

using LolloGPS.Data;
using LolloGPS.Data.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Utilz;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
// the polyline cannot be replaced with a route. The problem is, class MapRoute has no constructor and it is sealed. 
// The only way to create a route seems to be MapRouteFinder, which takes *ages* and it is not what we want here, either.
// Otherwise, you can look here: http://xamlmapcontrol.codeplex.com/SourceControl/latest#MapControl/MapPolyline.cs
// or here: http://phone.codeplex.com/SourceControl/latest
namespace LolloGPS.Core
{
    public sealed partial class LolloMap : UserControl, IGeoBoundingBoxProvider, IMapApController
    {
        #region properties
        // LOLLO TODO landmarks are still expensive to draw, no matter what I tried, so I set their limit to 256. It would be nice to have more though.
        // I tried MapIcon instead of Images: they are much slower loading but respond better to map movements! Ellipses are slower than images.

        internal const Double ScaleImageWidth = 300.0; //200.0;
        internal const string LandmarkTag = "Landmark";
        internal const double MinLat = -85.0511;
        internal const double MaxLat = 85.0511;
        internal const double MinLon = -180.0;
        internal const double MaxLon = 180.0;

        //private static MapPolyline _mapPolylineRoute0 = new MapPolyline() { StrokeColor = new Windows.UI.Color() { A = 96, B = 255 }, StrokeThickness = 8 };
        private static MapPolyline _mapPolylineRoute0 = new MapPolyline()
        {
            StrokeColor = ((SolidColorBrush)(App.Current.Resources["Route0Brush"])).Color,
            StrokeThickness = (Double)(App.Current.Resources["Route0Thickness"]),
        };
        //private static MapPolyline _mapPolylineHistory = new MapPolyline() { StrokeColor = new Windows.UI.Color() { A = 128, R = 255 }, StrokeThickness = 8 };
        private static MapPolyline _mapPolylineHistory = new MapPolyline()
        {
            StrokeColor = ((SolidColorBrush)(App.Current.Resources["HistoryBrush"])).Color,
            StrokeThickness = (Double)(App.Current.Resources["HistoryThickness"]),
        };
        private static Image _imageStartHistory = new Image() { Source = new BitmapImage(new Uri("ms-appx:///Assets/pointer_start-36.png")) { CreateOptions = BitmapCreateOptions.None }, Stretch = Stretch.None };
        private static Image _imageEndHistory = new Image() { Source = new BitmapImage(new Uri("ms-appx:///Assets/pointer_end-36.png")) { CreateOptions = BitmapCreateOptions.None }, Stretch = Stretch.None };
        private static Image _imageFlyoutPoint = new Image() { Source = new BitmapImage(new Uri("ms-appx:///Assets/pointer_current-36.png")) { CreateOptions = BitmapCreateOptions.None }, Stretch = Stretch.None };

        //private MapIcon _iconStartHistory = new MapIcon() { ZIndex = 999 }; MapIcons are not always shown, I use Images instead
        //private MapIcon _iconEndHistory = new MapIcon() { ZIndex = 999 };
        //private MapIcon _iconTarget = new MapIcon() { ZIndex = 999 };

        //private static Image _landmarkBaseImage = new Image() { Source = new BitmapImage(new Uri("ms-appx:///Assets/pointer_landmark-8.png")) { CreateOptions = BitmapCreateOptions.None }, Stretch = Stretch.None };
        // this uses a new "simple" icon with only 4 bits, so it's much faster to draw
        //private static Image _landmarkBaseImage = new Image() { Source = new BitmapImage(new Uri("ms-appx:///Assets/pointer_landmark_simple-8.png")) { CreateOptions = BitmapCreateOptions.None }, Stretch = Stretch.None };
        //private static Image _landmarkBaseImage = new Image() { Source = new BitmapImage(new Uri("ms-appx:///Assets/pointer_landmark_simple-16.png")) { CreateOptions = BitmapCreateOptions.None }, Stretch = Stretch.None };
        private static Image _landmarkBaseImage = new Image() { Source = new BitmapImage(new Uri("ms-appx:///Assets/pointer_landmark-20.png")) { CreateOptions = BitmapCreateOptions.None }, Stretch = Stretch.None };
        private static List<Image> _landmarkImages = new List<Image>();

        public PersistentData MyPersistentData { get { return App.PersistentData; } }
        public RuntimeData MyRuntimeData { get { return App.MyRuntimeData; } }
        private LolloMap_VM _myVM = null;
        public LolloMap_VM MyVM { get { return _myVM; } }

        private static WeakReference _myMapInstance = null;
        internal static MapControl GetMapControlInstance() // for the converter
        {
            if (_myMapInstance != null && _myMapInstance.Target != null && _myMapInstance.Target is MapControl) return _myMapInstance.Target as MapControl;
            else return null;
        }
        #endregion properties

        #region construct and dispose
        public LolloMap()
        {
            InitializeComponent();
            _myVM = new LolloMap_VM(MyMap.TileSources, this as IGeoBoundingBoxProvider, this as IMapApController);

            _myMapInstance = new WeakReference(MyMap);

            MyMap.Style = MyPersistentData.MapStyle;
            MyMap.DesiredPitch = 0.0;
            MyMap.Heading = 0;
            MyMap.TrafficFlowVisible = false;
            MyMap.LandmarksVisible = true;
            MyMap.MapServiceToken = "t8Ko1RpGcknITinQoF1IdA"; // "b77a5c561934e089";
            MyMap.PedestrianFeaturesVisible = true;
            MyMap.ColorScheme = MapColorScheme.Light; //.Dark
            //MyMap.MapElements.Clear(); // no!

            //_iconStartHistory.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/pointer_start-36.png", UriKind.Absolute));
            //_iconEndHistory.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/pointer_end-36.png", UriKind.Absolute));
            //_iconTarget.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/pointer_target-36.png", UriKind.Absolute));
            //_mapTileManager = new MapTileManager(this);
#if NOSTORE && !TRIALTESTING
            MyRuntimeData.IsTrial = false;
#endif
        }
        /// <summary>
        /// This is the only Activate that is async. It cannot take too long, it merely centers and zooms the map.
        /// Otherwise, we don't want to hog the UI thread with our Activate() !!
        /// </summary>
        /// <returns></returns>
        public async Task ActivateAsync()
        {
            try
            {
                MyMap.Style = MyPersistentData.MapStyle; // maniman
                await RestoreViewAsync();
                _myVM.Activate();

                DrawHistory();
                DrawRoute0();
                DrawLandmarks();

                AddHandlerThisEvents();
            }
            catch (Exception ex)
            {
                await Logger.AddAsync(ex.ToString(), Logger.ForegroundLogFilename).ConfigureAwait(false);
            }
        }
        public void Deactivate()
        {
            try
            {
                RemoveHandlerThisEvents();
                // save last map settings
                try
                {
                    MyPersistentData.MapLastLat = MyMap.Center.Position.Latitude;
                    MyPersistentData.MapLastLon = MyMap.Center.Position.Longitude;
                    MyPersistentData.MapLastHeading = MyMap.Heading;
                    MyPersistentData.MapLastPitch = MyMap.Pitch;
                    MyPersistentData.MapLastZoom = MyMap.ZoomLevel;
                }
                catch (Exception ex)
                {
                    Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
                }
                _myVM.Deactivate();
                MyPointInfoPanel.Deactivate();
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }
        #endregion construct and dispose

        #region services
        private async Task RestoreViewAsync()
        {
            try
            {
                Geopoint gp = new Geopoint(new BasicGeoposition() { Latitude = MyPersistentData.MapLastLat, Longitude = MyPersistentData.MapLastLon });
                await MyMap.TrySetViewAsync(gp, MyPersistentData.MapLastZoom, MyPersistentData.MapLastHeading, MyPersistentData.MapLastPitch, MapAnimationKind.None).AsTask().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }

        private async void CentreOnCurrent()
        {
            try
            {
                Geopoint newCentre = null;
                if (MyPersistentData.Current != null && !MyPersistentData.Current.IsEmpty())
                {
                    newCentre = new Geopoint(new BasicGeoposition() { Latitude = MyPersistentData.Current.Latitude, Longitude = MyPersistentData.Current.Longitude });
                }
                else
                {
                    newCentre = new Geopoint(new BasicGeoposition() { Latitude = 0.0, Longitude = 0.0 });
                }
                await MyMap.TrySetViewAsync(newCentre).AsTask().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }
        private async Task CentreAsync(Collection<PointRecord> coll)
        {
            try
            {
                if (coll == null || coll.Count < 1) return;
                else if (coll.Count == 1 && coll[0] != null && !coll[0].IsEmpty())
                {
                    Geopoint target = new Geopoint(new BasicGeoposition() { Latitude = coll[0].Latitude, Longitude = coll[0].Longitude });
                    await MyMap.TrySetViewAsync(target); //, CentreZoomLevel);
                }
                else if (coll.Count > 1)
                {
                    Double _minLongitude = default(Double);
                    Double _maxLongitude = default(Double);
                    Double _minLatitude = default(Double);
                    Double _maxLatitude = default(Double);

                    _minLongitude = coll.Min(a => a.Longitude);
                    _maxLongitude = coll.Max(a => a.Longitude);
                    _minLatitude = coll.Min(a => a.Latitude);
                    _maxLatitude = coll.Max(a => a.Latitude);
                    //Boolean isOK = await MyMap.TrySetViewBoundsAsync(new GeoboundingBox(new BasicGeoposition() { Latitude = _maxLatitude, Longitude = _minLongitude }, new BasicGeoposition() { Latitude = _minLatitude, Longitude = _maxLongitude }), new Thickness(20), Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Default);

                    await MyMap.TrySetViewBoundsAsync(new GeoboundingBox(
                        new BasicGeoposition() { Latitude = _maxLatitude, Longitude = _minLongitude },
                        new BasicGeoposition() { Latitude = _minLatitude, Longitude = _maxLongitude }),
                        new Thickness(20), //this is the margin to use in the view
                        Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Default
                        ).AsTask();
                }
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }
        async public Task CentreOnLandmarksAsync()
        {
            await CentreAsync(MyPersistentData.Landmarks);
        }
        async public Task CentreOnRoute0Async()
        {
            await CentreAsync(MyPersistentData.Route0);
        }
        async public Task CentreOnHistoryAsync()
        {
            await CentreAsync(MyPersistentData.History);
        }
        public async Task CentreOnTargetAsync()
        {
            try
            {
                if (MyPersistentData != null && MyPersistentData.Target != null)
                {
                    Geopoint location = new Geopoint(new BasicGeoposition()
                    {
                        Altitude = MyPersistentData.Target.Altitude,
                        Latitude = MyPersistentData.Target.Latitude,
                        Longitude = MyPersistentData.Target.Longitude
                    });
                    await MyMap.TrySetViewAsync(location); //, CentreZoomLevel);
                }
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }
        private async Task CentreOnSelectedPointAsync()
        {
            try
            {
                if (MyPersistentData != null && MyPersistentData.Selected != null)
                {
                    Geopoint location = new Geopoint(new BasicGeoposition() { Latitude = MyPersistentData.Selected.Latitude, Longitude = MyPersistentData.Selected.Longitude });
                    await MyMap.TrySetViewAsync(location); //, CentreZoomLevel);
                }
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }
        public void Goto2D()
        {
            MyMap.DesiredPitch = 0.0;
            MyMap.Heading = 0.0;
        }

        private void DrawHistory()
        {
            try
            {
                List<BasicGeoposition> basicGeoPositions = new List<BasicGeoposition>();
                try
                {
                    foreach (var item in MyPersistentData.History)
                    {
                        basicGeoPositions.Add(new BasicGeoposition() { Altitude = item.Altitude, Latitude = item.Latitude, Longitude = item.Longitude });
                    }
                }
                catch (OutOfMemoryException)
                {
                    var howMuchMemoryLeft = GC.GetTotalMemory(true); // LOLLO this is probably too late! Let's hope it does not happen since MyPersistentData puts a limit on the points.
                }

                if (basicGeoPositions.Count > 0)
                {
                    _mapPolylineHistory.Path = new Geopath(basicGeoPositions); // instead of destroying and redoing, it would be nice to just add the latest point; 
                                                                               // stupidly, _mapPolylineRoute0.Path.Positions is an IReadOnlyList.
                    MapControl.SetLocation(_imageStartHistory, new Geopoint(basicGeoPositions[0]));
                    MapControl.SetLocation(_imageEndHistory, new Geopoint(basicGeoPositions[basicGeoPositions.Count - 1]));
                    //_iconStartHistory.Location = new Geopoint(basicGeoPositions[0]);
                    //_iconEndHistory.Location = new Geopoint(basicGeoPositions[basicGeoPositions.Count - 1]);
                }
                //Better even: use binding; sadly, it is broken for the moment
                else
                {
                    BasicGeoposition lastGeoposition = new BasicGeoposition() { Altitude = MyPersistentData.Current.Altitude, Latitude = MyPersistentData.Current.Latitude, Longitude = MyPersistentData.Current.Longitude };
                    basicGeoPositions.Add(lastGeoposition);
                    _mapPolylineHistory.Path = new Geopath(basicGeoPositions);
                    MapControl.SetLocation(_imageStartHistory, new Geopoint(lastGeoposition));
                    MapControl.SetLocation(_imageEndHistory, new Geopoint(lastGeoposition));
                    //_iconStartHistory.Location = new Geopoint(lastGeoposition);
                    //_iconEndHistory.Location = new Geopoint(lastGeoposition);
                }

                MapControl.SetNormalizedAnchorPoint(_imageStartHistory, new Point(0.5, 0.625));
                MapControl.SetNormalizedAnchorPoint(_imageEndHistory, new Point(0.5, 0.625));
                //_iconStartHistory.NormalizedAnchorPoint = new Point(0.5, 0.625);
                //_iconEndHistory.NormalizedAnchorPoint = new Point(0.5, 0.625);

                if (!MyMap.MapElements.Contains(_mapPolylineHistory)) MyMap.MapElements.Add(_mapPolylineHistory);
                if (!MyMap.Children.Contains(_imageStartHistory)) MyMap.Children.Add(_imageStartHistory);
                if (!MyMap.Children.Contains(_imageEndHistory)) MyMap.Children.Add(_imageEndHistory);
                //if (!MyMap.MapElements.Contains(_iconStartHistory)) MyMap.MapElements.Add(_iconStartHistory);
                //if (!MyMap.MapElements.Contains(_iconEndHistory)) MyMap.MapElements.Add(_iconEndHistory);
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }

        private void DrawRoute0()
        {
            try
            {
                List<BasicGeoposition> basicGeoPositions = new List<BasicGeoposition>();
                try
                {
                    foreach (var item in MyPersistentData.Route0)
                    {
                        basicGeoPositions.Add(new BasicGeoposition() { Altitude = item.Altitude, Latitude = item.Latitude, Longitude = item.Longitude });
                    }
                }
                catch (OutOfMemoryException)
                {
                    var howMuchMemoryLeft = GC.GetTotalMemory(true); // LOLLO this is probably too late! Let's hope it does not happen since MyPersistentData puts a limit on the points.
                }

                if (basicGeoPositions.Count > 0)
                {
                    _mapPolylineRoute0.Path = new Geopath(basicGeoPositions); // instead of destroying and redoing, it would be nice to just add the latest point; 
                }                                                             // stupidly, _mapPolylineRoute0.Path.Positions is an IReadOnlyList.
                                                                              //Better even: use binding; sadly, it is broken for the moment
                else
                {
                    BasicGeoposition lastGeoposition = new BasicGeoposition() { Altitude = MyPersistentData.Current.Altitude, Latitude = MyPersistentData.Current.Latitude, Longitude = MyPersistentData.Current.Longitude };
                    basicGeoPositions.Add(lastGeoposition);
                    _mapPolylineRoute0.Path = new Geopath(basicGeoPositions);
                }
                if (!MyMap.MapElements.Contains(_mapPolylineRoute0))
                {
                    MyMap.MapElements.Add(_mapPolylineRoute0);
                }
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }
        private void DrawLandmarks()
        {
            try
            {
                // await Dispatcher.RunIdleAsync((callback) => InitLandmarks()); slow!
                if (!InitLandmarks()) // 1600 msec for 256 landmarks
                {
                    Debug.WriteLine("No landmarks to be drawn, skipping");
                    return;
                }
                // make geopoints
#if DEBUG
                Stopwatch sw0 = new Stopwatch(); sw0.Start();
#endif
                List<Geopoint> geoPoints = new List<Geopoint>();
                try
                {
                    foreach (var item in MyPersistentData.Landmarks)
                    {
                        geoPoints.Add(new Geopoint(new BasicGeoposition() { Altitude = item.Altitude, Latitude = item.Latitude, Longitude = item.Longitude }));
                    }
                }
                catch (OutOfMemoryException)
                {
                    var howMuchMemoryLeft = GC.GetTotalMemory(true); // LOLLO this is probably too late! Let's hope it does not happen since MyPersistentData puts a limit on the points.
                }
#if DEBUG
                sw0.Stop(); Debug.WriteLine("Making geopoints for landmarks took " + sw0.ElapsedMilliseconds + " msec");
                sw0.Restart();
#endif
                try
                {

                    for (int i = 0; i < geoPoints.Count; i++)
                    {
                        _landmarkImages[i].Visibility = Visibility.Visible;
                        MapControl.SetLocation(_landmarkImages[i], geoPoints[i]);
                        MapControl.SetNormalizedAnchorPoint(_landmarkImages[i], new Point(0.5, 0.5));
                    }
                    for (int i = geoPoints.Count; i < PersistentData.MaxRecordsInLandmarks; i++)
                    {
                        _landmarkImages[i].Visibility = Visibility.Collapsed;
                    }
                }
                catch (OutOfMemoryException)
                {
                    var howMuchMemoryLeft = GC.GetTotalMemory(true); // LOLLO this is probably too late! Let's hope it does not happen since MyPersistentData puts a limit on the points.
                }
#if DEBUG
                sw0.Stop(); Debug.WriteLine("attaching icons to map took " + sw0.ElapsedMilliseconds + " msec");
#endif
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }
        private bool InitLandmarks()
        {
#if DEBUG
            Stopwatch sw0 = new Stopwatch(); sw0.Start();
#endif
            bool isInit = false;
            if (_landmarkImages.Count == 0 && MyPersistentData.Landmarks.Count > 0) // only init when you really need it
            {
                Debug.WriteLine("InitLandmarks() is initialising the landmarks, because there really are some");
                for (int i = 0; i < PersistentData.MaxRecordsInLandmarks; i++)
                {
                    Image newIcon = new Image()
                    {
                        Source = _landmarkBaseImage.Source,
                        Stretch = Stretch.None,
                        Tag = LandmarkTag,
                        Visibility = Visibility.Collapsed,
                    };
                    MyMap.Children.Add(newIcon);

                    _landmarkImages.Add(newIcon);
                }
                isInit = true;
            }
            else if (_landmarkImages.Count > 0)

            {
                isInit = true;
            }
#if DEBUG
            sw0.Stop(); Debug.WriteLine("Initialising landmarks took " + sw0.ElapsedMilliseconds + " msec");
#endif
            return isInit;
        }
        private void HideFlyoutPoint()
        {
            _imageFlyoutPoint.Visibility = Visibility.Collapsed;

        }
        private void DrawFlyoutPoint()
        {
            if (MyPersistentData.Selected != null)
            {
                _imageFlyoutPoint.Visibility = Visibility.Visible;

                MapControl.SetLocation(_imageFlyoutPoint, new Geopoint(new BasicGeoposition() { Altitude = 0.0, Latitude = MyPersistentData.Selected.Latitude, Longitude = MyPersistentData.Selected.Longitude }));
                MapControl.SetNormalizedAnchorPoint(_imageFlyoutPoint, new Point(0.5, 0.625));

                if (!MyMap.Children.Contains(_imageFlyoutPoint)) MyMap.Children.Add(_imageFlyoutPoint);
            }
        }
        #endregion services

        #region IGeoBoundingBoxProvider
        public async Task<GeoboundingBox> GetMinMaxLatLonAsync()
        {
            GeoboundingBox output = null;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, delegate
            {
                // double minLat = default(double); double maxLat = default(double); double minLon = default(double); double maxLon = default(double);

                Geopoint topLeftGeopoint = null;
                Geopoint topRightGeopoint = null;
                Geopoint bottomLeftGeopoint = null;
                Geopoint bottomRightGeopoint = null;
                try
                {
                    MyMap.GetLocationFromOffset(new Windows.Foundation.Point(0.0, 0.0), out topLeftGeopoint);
                    MyMap.GetLocationFromOffset(new Windows.Foundation.Point(MyMap.ActualWidth, 0.0), out topRightGeopoint);
                    MyMap.GetLocationFromOffset(new Windows.Foundation.Point(0.0, MyMap.ActualHeight), out bottomLeftGeopoint);
                    MyMap.GetLocationFromOffset(new Windows.Foundation.Point(MyMap.ActualWidth, MyMap.ActualHeight), out bottomRightGeopoint);

                    double minLat = Math.Min(Math.Min(Math.Min(topLeftGeopoint.Position.Latitude, topRightGeopoint.Position.Latitude), bottomLeftGeopoint.Position.Latitude), bottomRightGeopoint.Position.Latitude);
                    double maxLat = Math.Max(Math.Max(Math.Max(topLeftGeopoint.Position.Latitude, topRightGeopoint.Position.Latitude), bottomLeftGeopoint.Position.Latitude), bottomRightGeopoint.Position.Latitude);
                    double minLon = Math.Min(Math.Min(Math.Min(topLeftGeopoint.Position.Longitude, topRightGeopoint.Position.Longitude), bottomLeftGeopoint.Position.Longitude), bottomRightGeopoint.Position.Longitude);
                    double maxLon = Math.Max(Math.Max(Math.Max(topLeftGeopoint.Position.Longitude, topRightGeopoint.Position.Longitude), bottomLeftGeopoint.Position.Longitude), bottomRightGeopoint.Position.Longitude);

                    AdjustMinMaxLatLon(ref minLat, ref maxLat, ref minLon, ref maxLon);

                    output = new GeoboundingBox(new BasicGeoposition() { Latitude = maxLat, Longitude = minLon }, new BasicGeoposition() { Latitude = minLat, Longitude = maxLon });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in LolloMap.GetMinMaxLatLonAsync(): " + ex.Message + ex.StackTrace);
                    output = null;
                }
            }).AsTask().ConfigureAwait(false);
            return output;
        }
        private static void AdjustMinMaxLatLon(ref double minLat, ref double maxLat, ref double minLon, ref double maxLon)
        {
            if (minLat < MinLat) minLat = MinLat;
            if (maxLat < MinLat) maxLat = MinLat;
            if (minLat > MaxLat) minLat = MaxLat;
            if (maxLat > MaxLat) maxLat = MaxLat;

            if (minLat > maxLat) LolloMath.Swap(ref minLat, ref maxLat);

            while (minLon < MinLon) minLon += 360.0;
            while (maxLon < MinLon) maxLon += 360.0;
            while (minLon > MaxLon) minLon -= 360.0;
            while (maxLon > MaxLon) maxLon -= 360.0;

            if (minLon > maxLon) LolloMath.Swap(ref minLon, ref maxLon);
        }
        public BasicGeoposition GetCentre()
        {
            return MyMap.Center.Position;
        }
        #endregion IGeoBoundingBoxProvider

        #region event handling
        // LOLLO TODO if we have a map source selected, can we check if this source allows zooming so much? It seems not, even with win 10.
        private void OnMap_Tapped(MapControl sender, MapInputEventArgs args)
        {
            try
            {
                if (args != null && args.Position != null) OpenPointDetailsPopup(args.Position.X, args.Position.Y);
            }
            catch (Exception ex) // there may be errors if I tap in an awkward place, such as the arctic
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }
        private void OnMap_RoutedTapped(object sender, TappedRoutedEventArgs args)
        {
            if (args.Handled) return;
            args.Handled = true;
            try
            {
                var tappedPoint = args.GetPosition(MyMap);
                if (tappedPoint != null) OpenPointDetailsPopup(tappedPoint.X, tappedPoint.Y);
            }
            catch (Exception ex) // there may be errors if I tap in an awkward place, such as the arctic
            {
                Logger.Add_TPL(ex.ToString(), Logger.ForegroundLogFilename);
            }
        }
        private void OpenPointDetailsPopup(double tappedScreenPointX, double tappedScreenPointY)
        {
            // pick up the four points delimiting a square centered on the display point that was tapped. Cull the square so it fits into the displayed area.
            Point topLeftPoint = new Point(Math.Max(tappedScreenPointX - MyPersistentData.TapTolerance, 0), Math.Max(tappedScreenPointY - MyPersistentData.TapTolerance, 0));
            Point topRightPoint = new Point(Math.Min(tappedScreenPointX + MyPersistentData.TapTolerance, MyMap.ActualWidth), Math.Max(tappedScreenPointY - MyPersistentData.TapTolerance, 0));
            Point bottomLeftPoint = new Point(Math.Max(tappedScreenPointX - MyPersistentData.TapTolerance, 0), Math.Min(tappedScreenPointY + MyPersistentData.TapTolerance, MyMap.ActualHeight));
            Point bottomRightPoint = new Point(Math.Min(tappedScreenPointX + MyPersistentData.TapTolerance, MyMap.ActualWidth), Math.Min(tappedScreenPointY + MyPersistentData.TapTolerance, MyMap.ActualHeight));

            Geopoint topLeftGeoPoint = null;
            Geopoint topRightGeoPoint = null;
            Geopoint bottomLeftGeoPoint = null;
            Geopoint bottomRightGeoPoint = null;

            // pick up the geographic coordinates of those points
            MyMap.GetLocationFromOffset(topLeftPoint, out topLeftGeoPoint);
            MyMap.GetLocationFromOffset(topRightPoint, out topRightGeoPoint);
            MyMap.GetLocationFromOffset(bottomLeftPoint, out bottomLeftGeoPoint);
            MyMap.GetLocationFromOffset(bottomRightPoint, out bottomRightGeoPoint);

            // work out the maxes and mins, so you can ignore rotation, pitch etc
            double minLat = Math.Min(bottomRightGeoPoint.Position.Latitude, Math.Min(bottomLeftGeoPoint.Position.Latitude, Math.Min(topLeftGeoPoint.Position.Latitude, topRightGeoPoint.Position.Latitude)));
            double maxLat = Math.Max(bottomRightGeoPoint.Position.Latitude, Math.Max(bottomLeftGeoPoint.Position.Latitude, Math.Max(topLeftGeoPoint.Position.Latitude, topRightGeoPoint.Position.Latitude)));
            double minLon = Math.Min(bottomRightGeoPoint.Position.Longitude, Math.Min(bottomLeftGeoPoint.Position.Longitude, Math.Min(topLeftGeoPoint.Position.Longitude, topRightGeoPoint.Position.Longitude)));
            double maxLon = Math.Max(bottomRightGeoPoint.Position.Longitude, Math.Max(bottomLeftGeoPoint.Position.Longitude, Math.Max(topLeftGeoPoint.Position.Longitude, topRightGeoPoint.Position.Longitude)));

            //if a point falls within the square, show its details
            List<PointRecord> selectedRecords = new List<PointRecord>();
            List<PersistentData.Tables> selectedSeriess = new List<PersistentData.Tables>();
            foreach (var item in MyPersistentData.History)
            {
                if (item.Latitude < maxLat && item.Latitude > minLat && item.Longitude > minLon && item.Longitude < maxLon)
                {
                    selectedRecords.Add(item);
                    selectedSeriess.Add(PersistentData.Tables.History);
                    break; // max 1 record each series
                }
            }
            foreach (var item in MyPersistentData.Route0)
            {
                if (item.Latitude < maxLat && item.Latitude > minLat && item.Longitude > minLon && item.Longitude < maxLon)
                {
                    selectedRecords.Add(item);
                    selectedSeriess.Add(PersistentData.Tables.Route0);
                    break; // max 1 record each series
                }
            }
            foreach (var item in MyPersistentData.Landmarks)
            {
                if (item.Latitude < maxLat && item.Latitude > minLat && item.Longitude > minLon && item.Longitude < maxLon)
                {
                    selectedRecords.Add(item);
                    selectedSeriess.Add(PersistentData.Tables.Landmarks);
                    break; // max 1 record each series
                }
            }
            if (selectedRecords.Count > 0)
            {
                Task vibrate = Task.Run(() => App.ShortVibration());
                MyPointInfoPanel.SetDetails(selectedRecords, selectedSeriess);
                // SelectedPointFlyout.ShowAt(MyMap);
                SelectedPointPopup.IsOpen = true;
            }
        }
        private void OnMap_RoutedHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;
            Task vibrate = Task.Run(() => App.ShortVibration());

            CentreOnCurrent();
        }

        private void OnMap_Holding(MapControl sender, MapInputEventArgs args)
        {
            Task vibrate = Task.Run(() => App.ShortVibration());

            CentreOnCurrent();
        }

        private async void OnInfoPanelPointChanged(object sender, EventArgs e)
        {
            if (MyPersistentData.IsSelectedSeriesNonNullAndNonEmpty())
            {
                DrawFlyoutPoint();
                // if (MyPersistentData.IsCentreOnCurrent) 
                await CentreOnSelectedPointAsync();
            }
            else
            {
                SelectedPointPopup.IsOpen = false;
            }
        }

        private async void OnAim_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;
            Task vibrate = Task.Run(() => App.ShortVibration());

            await MyVM.AddMapCentreToLandmarks();
            if (MyPersistentData.IsShowAimOnce)
            {
                MyPersistentData.IsShowAim = false;
            }
        }

        private void OnAim_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;

            MyPersistentData.IsShowAim = false;
        }

        private void OnAim_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;

            MyPersistentData.IsShowAim = false;
        }

        private async void OnProvider_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;

            try
            {
                if (!string.IsNullOrWhiteSpace(MyPersistentData.CurrentTileSource.ProviderUriString) && MyRuntimeData.IsConnectionAvailable)
                {
                    await Launcher.LaunchUriAsync(new Uri(MyPersistentData.CurrentTileSource.ProviderUriString, UriKind.Absolute));
                }
            }
            catch (Exception) { }
        }

        private void OnBackKeyPressed(object sender, EventArgs e)
        {
            SelectedPointPopup.IsOpen = false;
        }

        private void OnInfoPanelClosed(object sender, object e)
        {
            HideFlyoutPoint();
        }

        private void OnPersistentData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PersistentData.MapStyle))
            {
                MyMap.Style = MyPersistentData.MapStyle;
            }
            //else if (e.PropertyName == "IsCentreOnCurrent")
            //{
            //    if (MyPersistentData != null && MyPersistentData.IsCentreOnCurrent)
            //    {
            //        CentreOnCurrent();
            //    }
            //}
        }
        private void OnPersistentData_CurrentChanged(object sender, EventArgs e)
        {
            // I must not run to the current point when starting, I want to stick to the last frame when last suspended instead.
            if (MyPersistentData != null && MyPersistentData.IsCentreOnCurrent && (SelectedPointPopup == null || !SelectedPointPopup.IsOpen))
            {                
                CentreOnCurrent();
            }
        }

        private void OnLandmarks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DrawLandmarks();
        }

        private void OnRoute0_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DrawRoute0();
        }

        private void OnHistory_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DrawHistory();
        }

        private Boolean _isHandlerActive = false;
        private Delegate _tappedHandler = null;
        private Delegate _holdingHandler = null;

        private void AddHandlerThisEvents()
        {
            if (MyPersistentData != null && !_isHandlerActive)
            {
                MyPersistentData.PropertyChanged += OnPersistentData_PropertyChanged;
                MyPersistentData.CurrentChanged += OnPersistentData_CurrentChanged;
                MyPersistentData.History.CollectionChanged += OnHistory_CollectionChanged;
                MyPersistentData.Route0.CollectionChanged += OnRoute0_CollectionChanged;
                MyPersistentData.Landmarks.CollectionChanged += OnLandmarks_CollectionChanged;

                if (_tappedHandler == null) _tappedHandler = new TappedEventHandler(OnMap_RoutedTapped);
                MyMap.AddHandler(TappedEvent, _tappedHandler, true);
                MyMap.MapTapped += OnMap_Tapped;

                if (_holdingHandler == null) _holdingHandler = new HoldingEventHandler(OnMap_RoutedHolding);
                MyMap.AddHandler(HoldingEvent, _holdingHandler, true);
                MyMap.MapHolding += OnMap_Holding;

                _isHandlerActive = true;
            }
        }

        private void RemoveHandlerThisEvents()
        {
            if (MyPersistentData != null)
            {
                MyPersistentData.PropertyChanged -= OnPersistentData_PropertyChanged;
                MyPersistentData.CurrentChanged -= OnPersistentData_CurrentChanged;
                MyPersistentData.History.CollectionChanged -= OnHistory_CollectionChanged;
                MyPersistentData.Route0.CollectionChanged -= OnRoute0_CollectionChanged;
                MyPersistentData.Landmarks.CollectionChanged -= OnLandmarks_CollectionChanged;

                if (_tappedHandler != null) MyMap.RemoveHandler(TappedEvent, _tappedHandler);
                MyMap.MapTapped -= OnMap_Tapped;

                if (_holdingHandler != null) MyMap.RemoveHandler(HoldingEvent, _holdingHandler);
                MyMap.MapHolding -= OnMap_Holding;

                _isHandlerActive = false;
            }
        }
        #endregion event handling
    }

    #region converters
    public class HeadingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return 0.0;
            Double mapHeading = 0.0;
            Double.TryParse(value.ToString(), out mapHeading);
            return -mapHeading;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new Exception("should never get here");
        }
    }

    public class ScaleSizeConverter : IValueConverter
    {
        //private const Double VerticalHalfCircM = 20004000.0;
        private const Double LatitudeToMetres = 111133.33333333333; //vertical half circumference of earth / 180 degrees
        private static Double _lastZoomScale = 0.0; //remember this to avoid repeating the same calculation
        private static Double _imageScaleTransform = 1.0;
        private static String _distRoundedFormatted = "1 m";
        private static Double _rightLabelX = LolloMap.ScaleImageWidth;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MapControl mapControl = LolloMap.GetMapControlInstance();
            Double currentZoomScale = 0.0;
            if (mapControl != null)
            {
                Double.TryParse(value.ToString(), out currentZoomScale);

                if (currentZoomScale != _lastZoomScale)
                {
                    try
                    {
                        Calc(mapControl);
                        _lastZoomScale = currentZoomScale; //remember this to avoid repeating the same calculation
                    }
                    catch (Exception) { } // there may be exceptions if I am in a very awkward place in the map, such as the arctic
                }
            }
            String param = parameter.ToString();
            if (param == "imageScaleTransform")
            {
                return _imageScaleTransform;
            }
            else if (param == "distRounded")
            {
                return _distRoundedFormatted;
            }
            else if (param == "rightLabelX")
            {
                return _rightLabelX;
            }
            else if (param == "techZoom")
            {
                return currentZoomScale.ToString("zoom #0.#", CultureInfo.CurrentUICulture);
            }
            else if (param == "techZoomX")
            {
                return _rightLabelX + 60;
            }
            Debug.WriteLine("ERROR: XAML used a wrong parameter, or no parameter");
            return 1.0; //should never get here
        }

        private static void Calc(MapControl mapControl)
        {
            if (mapControl != null)
            {
                //we work out the distance moving along the meridians, because the parallels are always at the same distance at all latitudes
                Double halfMapWidth = mapControl.ActualWidth / 2.0;
                Double halfMapHeight = mapControl.ActualHeight / 2.0;
                if (halfMapHeight <= 0 || halfMapWidth <= 0) return;

                Double headingRadians = mapControl.Heading * Math.PI / 180.0;
                Point pointN = new Point(halfMapWidth, halfMapHeight);
                Double barLength = 99.0; // LolloMap.scaleImageWidth - 1; //I use a shortish bar length so it always fits snugly in the MapControl
                                         //                    Point pointS = new Point(halfMapWidth, halfMapHeight + barLength);//this returns funny results when the map is turned: I must always measure along the meridians
                Point pointS = new Point(halfMapWidth + barLength * Math.Sin(headingRadians), halfMapHeight + barLength * Math.Cos(headingRadians));
                //Double checkIpotenusa = Math.Sqrt((pointN.X - pointS.X) * (pointN.X - pointS.X) + (pointN.Y - pointS.Y) * (pointN.Y - pointS.Y)); //remove when done testing
                Geopoint locationN = null;
                Geopoint locationS = null;
                mapControl.GetLocationFromOffset(pointN, out locationN);
                mapControl.GetLocationFromOffset(pointS, out locationS);
                Double dist = Math.Abs(locationN.Position.Latitude - locationS.Position.Latitude) * LatitudeToMetres * LolloMap.ScaleImageWidth / (barLength + 1); //need the abs for when the map is rotated
                String distStr = Math.Truncate(dist).ToString(CultureInfo.InvariantCulture);
                //work out next lower round distance because the scale must always be very round
                String distStrRounded = distStr.Substring(0, 1);
                for (int i = 0; i < distStr.Length - 1; i++)
                {
                    distStrRounded += "0";
                }
                Double distRounded = 0.0;
                Double.TryParse(distStrRounded, out distRounded);

                if (distRounded > 1000)
                {
                    _distRoundedFormatted = (distRounded / 1000).ToString() + " km"; //I could also use the string, but this is good for testing
                }
                else
                {
                    _distRoundedFormatted = distRounded.ToString() + " m"; //I could also use the string, but this is good for testing
                }
                if (dist != 0.0) _imageScaleTransform = distRounded / dist;
                else _imageScaleTransform = 999.999;
                _rightLabelX = _imageScaleTransform * LolloMap.ScaleImageWidth;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new Exception("should never get here");
        }
    }
    #endregion converters
}
