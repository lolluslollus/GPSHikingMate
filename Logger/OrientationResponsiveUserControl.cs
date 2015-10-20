using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace LolloBaseUserControls
{
    public class OrientationResponsiveUserControl : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] String propertyName = "")
        {
            var listener = PropertyChanged;
            if (listener != null)
            {
                listener(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion INotifyPropertyChanged

        #region construct and destroy
        public OrientationResponsiveUserControl()
            : base()
        {
            _appView = ApplicationView.GetForCurrentView();
            //_orientationSensor = SimpleOrientationSensor.GetDefault();
            //if (_orientationSensor != null) { _lastOrientation = _orientationSensor.GetCurrentOrientation(); }
            UseLayoutRounding = true;
            Loaded += OnLoadedInternal;
            Unloaded += OnUnloadedInternal;
        }
        //~OrientationResponsiveUserControl()
        //{
        //    try
        //    {
        //        Loaded -= OnLoadedInternal;
        //        Unloaded -= OnUnloadedInternal;
        //    }
        //    catch (Exception exc) { }
        //}
        #endregion construct and destroy
        /// <summary>
        /// Gets or sets the behaviour when the back key is pressed.
        /// If true, do not respond to the event directly but raise BackKeyPressed instead.
        /// If false, the framework will run its course.
        /// </summary>
        public bool IsOverrideBackKeyPressed
        {
            get { return (bool)GetValue(IsOverrideBackKeyPressedProperty); }
            set { SetValue(IsOverrideBackKeyPressedProperty, value); }
        }
        public static readonly DependencyProperty IsOverrideBackKeyPressedProperty =
            DependencyProperty.Register("IsOverrideBackKeyPressed", typeof(bool), typeof(OrientationResponsiveUserControl), new PropertyMetadata(false));

        #region appView
        private ApplicationView _appView = null;
        public ApplicationView AppView { get { return _appView; } }

        private void OnLoadedInternal(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AddHandlers();
            OnVisibleBoundsChanged(_appView, null);
            OnLoaded();
        }

        protected virtual void OnHardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (IsOverrideBackKeyPressed && Visibility == Windows.UI.Xaml.Visibility.Visible) // && ActualHeight > 0.0 && ActualWidth > 0.0)
            {
                e.Handled = true;
                RaiseBackKeyPressed();
            }
        }

        private void OnUnloadedInternal(object sender, RoutedEventArgs e)
        {
            OnUnloaded();
            RemoveHandlers();
        }
        protected virtual void OnLoaded()
        {

        }
        protected virtual void OnUnloaded()
        {

        }
        private Boolean _isHandlersActive = false;
        private void AddHandlers()
        {
            if (_isHandlersActive == false)
            {
                if (_appView != null) _appView.VisibleBoundsChanged += OnVisibleBoundsChanged;
                //if (_orientationSensor != null) _orientationSensor.OrientationChanged += OnSensor_OrientationChanged;
                HardwareButtons.BackPressed += OnHardwareButtons_BackPressed;
                _isHandlersActive = true;
            }
        }

        private void RemoveHandlers()
        {
            if (_appView != null) _appView.VisibleBoundsChanged -= OnVisibleBoundsChanged;
            //if (_orientationSensor != null) _orientationSensor.OrientationChanged -= OnSensor_OrientationChanged;
            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= OnHardwareButtons_BackPressed;
            _isHandlersActive = false;
        }

        protected virtual void OnVisibleBoundsChanged(ApplicationView sender, object args)
        {
            RaiseVisibleBoundsChanged(args);
        }
        /// <summary>
        /// Raised when the orientation changes, only if rotation for the app is enabled
        /// </summary>
        public event TypedEventHandler<ApplicationView, object> VisibleBoundsChanged;
        private void RaiseVisibleBoundsChanged(object args)
        {
            var listener = VisibleBoundsChanged;
            if (listener != null)
            {
                listener(_appView, args);
            }
        }
        /// <summary>
        /// Back key pressed
        /// </summary>
        public event EventHandler BackKeyPressed;
        private void RaiseBackKeyPressed()
        {
            var listener = BackKeyPressed;
            if (listener != null)
            {
                listener(this, EventArgs.Empty);
            }
        }
        #endregion appView

        // the following works but we don't need it
        //#region sensor
        //private SimpleOrientationSensor _orientationSensor;
        //private SimpleOrientation _lastOrientation;
        //protected virtual void OnSensor_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        //{
        //    if (_lastOrientation == null || args.Orientation != _lastOrientation)
        //    {
        //        bool mustRaise = false; // check this if you want to use the code at some point
        //        switch (args.Orientation)
        //        {
        //            case SimpleOrientation.Facedown:
        //                break;
        //            case SimpleOrientation.Faceup:
        //                break;
        //            case SimpleOrientation.NotRotated:
        //                break;
        //            case SimpleOrientation.Rotated180DegreesCounterclockwise:
        //                mustRaise = true; break;
        //            case SimpleOrientation.Rotated270DegreesCounterclockwise:
        //                mustRaise = true; break;
        //            case SimpleOrientation.Rotated90DegreesCounterclockwise:
        //                mustRaise = true; break;
        //            default:
        //                break;
        //        }
        //        _lastOrientation = args.Orientation;
        //        if (mustRaise) RaiseOrientationChanged(args);
        //    }
        //}
        ///// <summary>
        ///// Raised when the orientation changes, even if rotation for the app is disabled
        ///// </summary>
        //public event TypedEventHandler<SimpleOrientationSensor, SimpleOrientationSensorOrientationChangedEventArgs> OrientationChanged;
        //private void RaiseOrientationChanged(SimpleOrientationSensorOrientationChangedEventArgs args)
        //{
        //    var listener = OrientationChanged;
        //    if (listener != null)
        //    {
        //        listener(_orientationSensor, args);
        //    }
        //}
        //#endregion sensor
    }
}
