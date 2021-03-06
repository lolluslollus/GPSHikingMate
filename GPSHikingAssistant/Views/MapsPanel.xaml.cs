﻿using LolloBaseUserControls;
using LolloGPS.Data;
using LolloGPS.Data.Runtime;
using LolloListChooser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace LolloGPS.Core
{
    public sealed partial class MapsPanel : OrientationResponsiveUserControl
    {
        public PersistentData MyPersistentData { get { return App.PersistentData; } }
        public RuntimeData MyRuntimeData { get { return App.MyRuntimeData; } }

        private Main_VM _myVM = Main_VM.GetInstance();
        public Main_VM MyVM { get { return _myVM; } }

        public MapsPanel()
        {
            InitializeComponent();
        }

        private void OnGoto2D_Click(object sender, RoutedEventArgs e)
        {
            RaiseGoto2DRequested();
        }
        private void OnMapStyleButton_Click(object sender, RoutedEventArgs e)
        {
            MyPersistentData.CycleMapStyle();
        }

        private void OnClearMapCache_Click(object sender, RoutedEventArgs e)
        {
            if (_myVM.IsClearCacheEnabled) // this is redundant safety
            {
                ClearCacheChooser.IsPopupOpen = true;
            }
            else MyPersistentData.LastMessage = "Cache busy";
        }
        private void OnClearCacheChooser_ItemSelected(object sender, TextAndTag e)
        {
            if (e == null || e.Tag == null || e.Tag as TileSourceRecord == null) return;
            // clear cache if requested
            TileSourceRecord tag = (e.Tag as TileSourceRecord);
            if (!tag.IsNone && !tag.IsDefault)
            {
                Task delete = _myVM.TryClearCacheAsync(tag, false);
            }
        }

        private async void OnDownloadMap_Click(object sender, RoutedEventArgs e)
        {
            if (_myVM.IsLeechingEnabled) // this is redundant safety
            {
                // present a choice of zoom levels
                List<Tuple<int, int>> howManyTiles4DifferentZooms = await MyVM.GetHowManyTiles4DifferentZoomsAsync().ConfigureAwait(true);

                Collection<TextAndTag> tts = new Collection<TextAndTag>();
                foreach (var item in howManyTiles4DifferentZooms)
                {
                    if (item.Item2 <= TileDownloader.MaxTilesToLeech && item.Item1 > 0 && item.Item2 > 0)
                    {
                        string message = "Zoom  " + item.Item1 + " gets up to " + item.Item2 + " tiles";
                        tts.Add(new TextAndTag(message, item.Item1));
                    }

                }
                if (tts.Count > 0)
                {
                    ZoomLevelChooser.ItemsSource = tts;
                    ZoomLevelChooser.IsPopupOpen = true;
                }
                else
                {
                    _myVM.SetLastMessage_UI("No downloads possible for this area");
                    // TODO this happens when I zoom out and go north to the arctic.
                    // the error happens in MapControl.GetLocationFromOffset(). It seems diffivult to fix.
                    // Fix it: try and download at least a few tiles. Try fumbling with LolloMap.GetMinMaxLatLonAsync().
                }
            }
            else MyPersistentData.LastMessage = "Download busy";
        }
        private void OnZoomLevelChooser_ItemSelected(object sender, TextAndTag e)
        {
            if (e == null || e.Tag == null || !(e.Tag is int)) return;
            int maxZoom = (int)(e.Tag);
            MyPersistentData.SetIsTilesDownloadDesired(true, maxZoom);
            MyPersistentData.IsShowingPivot = false;
            //MyVM.IsShowingMapSourcePanel = false;
        }
        protected override void OnHardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            // only go ahead with Back if no popups are open, otherwise they will close and this will remain open
            if (!ClearCacheChooser.IsPopupOpen && !ZoomLevelChooser.IsPopupOpen && !MapSourceChooser.IsPopupOpen && !e.Handled)
            {
                base.OnHardwareButtons_BackPressed(sender, e);
            }
        }

        #region events
        public event EventHandler Goto2DRequested;
        private void RaiseGoto2DRequested()
        {
            var listener = Goto2DRequested;
            if (listener != null)
            {
                listener(this, EventArgs.Empty);
            }
        }
        #endregion events
    }
}
