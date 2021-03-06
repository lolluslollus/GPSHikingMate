﻿using LolloBaseUserControls;
using System;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace LolloListChooser
{
    public sealed partial class LolloListChooser : OrientationResponsiveUserControl
    {
        #region properties
        private const string DefaultPlaceholderText = "Select an item";
        private const string DefaultListHeaderText = "Choose an item";

        public Visibility SelectorVisibility
        {
            get { return (Visibility)GetValue(SelectorVisibilityProperty); }
            set { SetValue(SelectorVisibilityProperty, value); }
        }
        public static readonly DependencyProperty SelectorVisibilityProperty =
            DependencyProperty.Register("SelectorVisibility", typeof(Visibility), typeof(LolloListChooser), new PropertyMetadata(Visibility.Visible));

        public bool IsPopupOpen
        {
            get { return (bool)GetValue(IsPopupOpenProperty); }
            set { SetValue(IsPopupOpenProperty, value); }
        }
        public static readonly DependencyProperty IsPopupOpenProperty =
            DependencyProperty.Register("IsPopupOpen", typeof(bool), typeof(LolloListChooser), new PropertyMetadata(false, OnIsPopupOpen_PropertyChanged));
        private static void OnIsPopupOpen_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            LolloListChooser me = obj as LolloListChooser;
            bool newValue = (bool)(e.NewValue);
            if (me != null)
            {
                if (newValue) me.OpenPopupAfterIsPopupOpenChanged();
                else me.ClosePopupAfterIsPopupOpenChanged();
            }
        }

        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register("PlaceholderText", typeof(string), typeof(LolloListChooser), new PropertyMetadata(DefaultPlaceholderText, OnPlaceholderText_PropertyChanged));
        private static void OnPlaceholderText_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            LolloListChooser me = obj as LolloListChooser;
            string newValue = e.NewValue as string;
            if (me != null)
            {
                if (string.IsNullOrEmpty(me.Text))
                {
                    me.MyTextBlock.Text = newValue;
                }
            }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(LolloListChooser), new PropertyMetadata(null, OnText_PropertyChanged));
        private static void OnText_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            LolloListChooser me = obj as LolloListChooser;
            string newValue = e.NewValue as string;
            if (me != null)
            {
                if (newValue != null)
                {
                    me.MyTextBlock.Text = newValue;
                }
                else
                {
                    me.MyTextBlock.Text = me.PlaceholderText;
                }
            }
        }

        public string ListHeaderText
        {
            get { return (string)GetValue(ListHeaderTextProperty); }
            set { SetValue(ListHeaderTextProperty, value); }
        }
        public static readonly DependencyProperty ListHeaderTextProperty =
            DependencyProperty.Register("ListHeaderText", typeof(string), typeof(LolloListChooser), new PropertyMetadata(DefaultListHeaderText));

        public Style TextBlockStyle
        {
            get { return (Style)GetValue(TextBlockStyleProperty); }
            set { SetValue(TextBlockStyleProperty, value); }
        }
        public static readonly DependencyProperty TextBlockStyleProperty =
            DependencyProperty.Register("TextBlockStyle", typeof(Style), typeof(LolloListChooser), new PropertyMetadata(null));

        public Style AppBarButtonStyle
        {
            get { return (Style)GetValue(AppBarButtonStyleProperty); }
            set { SetValue(AppBarButtonStyleProperty, value); }
        }
        public static readonly DependencyProperty AppBarButtonStyleProperty =
            DependencyProperty.Register("AppBarButtonStyle", typeof(Style), typeof(LolloListChooser), new PropertyMetadata(null));

        public Style TextItemStyle
        {
            get { return (Style)GetValue(TextItemStyleProperty); }
            set { SetValue(TextItemStyleProperty, value); }
        }
        public static readonly DependencyProperty TextItemStyleProperty =
            DependencyProperty.Register("TextItemStyle", typeof(Style), typeof(LolloListChooser), new PropertyMetadata(null));

        public Collection<TextAndTag> ItemsSource
        {
            get { return (Collection<TextAndTag>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(Collection<TextAndTag>), typeof(LolloListChooser), new PropertyMetadata(null)); //, OnItemsSource_PropertyChanged));
        //private static void OnItemsSource_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        //{
        //    LolloListChooser me = obj as LolloListChooser;
        //    if (me != null && e.NewValue as Collection<TextAndTag> != null)
        //    {
        //        Collection<TextAndTag> newValue = (Collection<TextAndTag>)e.NewValue;
        //        if (newValue is ObservableCollection<TextAndTag>)
        //        {
        //            ObservableCollection<TextAndTag> newObservableValue = (ObservableCollection<TextAndTag>)newValue;
        //        }
        //    }
        //}

        public TextAndTag SelectedItem
        {
            get { return (TextAndTag)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(TextAndTag), typeof(LolloListChooser), new PropertyMetadata(null, OnSelectedItem_PropertyChanged));
        private static void OnSelectedItem_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            LolloListChooser me = obj as LolloListChooser;
            if (me != null)
            {
                if (e != null && e.NewValue != null && e.NewValue is TextAndTag)
                {
                    me.MyTextBlock.Text = (e.NewValue as TextAndTag).Text;
                }
                else
                {
                    me.MyTextBlock.Text = me.PlaceholderText;
                }
            }
        }
        /// <summary>
        /// Gets the index of the selected element
        /// </summary>
        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            private set { SetValue(SelectedIndexProperty, value); }
        }
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(LolloListChooser), new PropertyMetadata(-1));
        #endregion properties

        #region construct and dispose
        public LolloListChooser()
            : base()
        {
            this.IsOverrideBackKeyPressed = false;
            this.InitializeComponent();
            MyTextBlock.Text = PlaceholderText;
        }
        #endregion construct and dispose

        #region popup
        protected override void OnHardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (IsPopupOpen)
            {
                e.Handled = true;
                IsPopupOpen = false;
            }
            //else
            //{
            //    base.OnHardwareButtons_BackPressed(sender, e);
            //}
        }
        protected override void OnVisibleBoundsChanged(ApplicationView sender, object args)
        {
            if (IsPopupOpen)
            {
                IsPopupOpen = false;
                // UpdatePopupSizeAndPlacement(); TODO maybe this screws up, let's just close the popup for now
            }
            // base.OnVisibleBoundsChanged(sender, args);
        }
        /// <summary>
        /// Only call this in the IsPopupOpen change handler.
        /// Otherwise, change the dependency property IsPopupOpen.
        /// </summary>
        private void OpenPopupAfterIsPopupOpenChanged()
        {
            UpdatePopupSizeAndPlacement();
            MyPopup.IsOpen = true; // only change this property in the IsPopupOpen change handler. Otherwise, change the dependency property IsPopupOpen.
        }

        private void UpdatePopupSizeAndPlacement()
        {
            Rect availableBoundsWithinChrome = AppView.VisibleBounds;

            MyPoupGrid.Height = availableBoundsWithinChrome.Height;
            MyPoupGrid.Width = availableBoundsWithinChrome.Width;

            var transform = this.TransformToVisual(Window.Current.Content);
            var relativePoint = transform.TransformPoint(new Point(-availableBoundsWithinChrome.X, -availableBoundsWithinChrome.Y));
            // var relativePoint = transform.TransformPoint(new Point(0.0, 0.0));
            Canvas.SetLeft(MyPopup, -relativePoint.X);
            Canvas.SetTop(MyPopup, -relativePoint.Y);
        }

        /// <summary>
        /// Only call this in the IsPopupOpen change handler.
        /// Otherwise, change the dependency property IsPopupOpen.
        /// </summary>
        private void ClosePopupAfterIsPopupOpenChanged()
        {
            MyPopup.IsOpen = false; // only change this property in the IsPopupOpen change handler. Otherwise, change the dependency property IsPopupOpen.
        }

        private void OnMyPopup_Closed(object sender, object e)
        {
            IsPopupOpen = false;
        }
        #endregion popup

        #region event handlers
        private void OnMyTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (this.ItemsSource != null && !IsPopupOpen)
            {
                IsPopupOpen = true;
            }
        }

        //private void OnMyListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    this.SelectedItem = MyListView.SelectedItem as TextAndTag;
        //    this.SelectedIndex = MyListView.SelectedIndex;
        //    IsPopupOpen = false;
        //    RaiseSelectionChanged(sender, e);
        //}
        private void OnItemBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            IsPopupOpen = false;
            this.SelectedItem = MyListView.SelectedItem as TextAndTag;
            this.SelectedIndex = MyListView.SelectedIndex;
            RaiseItemSelected(sender, ((sender as FrameworkElement).DataContext as TextAndTag));
        }

        public event EventHandler<TextAndTag> ItemSelected;
        private void RaiseItemSelected(object sender, TextAndTag e)
        {
            var listener = ItemSelected;
            if (listener != null)
            {
                listener(sender, e);
            }
        }
        #endregion event handlers
    }

    public class TextAndTag
    {
        private string _text = "";
        public string Text { get { return _text; } set { _text = value; } }

        private object _tag = null;
        public object Tag { get { return _tag; } set { _tag = value; } }

        public TextAndTag(string text, object tag)
        {
            _text = text;
            _tag = tag;
        }
        public static Collection<TextAndTag> CreateCollection(string[] texts, Array tags)
        {
            if (texts == null || tags == null || texts.Length != tags.Length) throw new ArgumentException("Texts and tags must not be null and must have the same length");
            Collection<TextAndTag> output = new Collection<TextAndTag>();
            for (int i = 0; i < texts.Length; i++)
            {
                output.Add(new TextAndTag(texts[i], tags.GetValue(new int[1] { i })));
            }
            return output;
        }
    }
}
