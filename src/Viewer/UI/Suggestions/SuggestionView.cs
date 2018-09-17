﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;

namespace Viewer.UI.Suggestions
{
    /// <summary>
    /// This form shows suggestions in a custom draw control. 
    /// </summary>
    /// <example>
    /// This example attaches this control to a text box and loads suggestions as the text in
    /// the text box changes:
    /// 
    /// > [!IMPORTANT]
    /// > This class implements the <see cref="IDisposable"/> interface. You should dispose it once
    /// > you are done with it. This example omits cleanup such as disposing the control and
    /// > unsubsribing from events for clarity.
    /// 
    /// <code>
    /// var textbox = ...;
    /// var suggestions = new SuggestionView(textbox);
    /// textbox.TextChanged += (sender, args) =>
    /// {
    ///     suggestions.Items = LoadItemsFromDatabase(...);
    ///     suggestions.ShowAtCurrentControl();
    /// }
    /// </code>
    ///
    /// > [!NOTE]
    /// > The <see cref="ShowAtCurrentControl"/> method will only show suggestions if the Items
    /// > list is not empty. If there is no control attached, this will be a no-op.
    /// </example>
    internal partial class SuggestionView : Form
    {
        private readonly SuggestionControl _suggestionControl;
        private readonly List<SuggestionItem> _items = new List<SuggestionItem>();

        /// <summary>
        /// List of suggestions shown to the user. 
        /// </summary>
        public IEnumerable<SuggestionItem> Items
        {
            get => _items;
            set
            {
                _suggestionControl.Invalidate();
                _items.Clear();
                if (value == null)
                {
                    OnItemsChanged();
                    return;
                }
                
                foreach (var item in value)
                {
                    _items.Add(item);
                }
                
                OnItemsChanged();
            }
        }

        private int _maxVisibleItemCount = 10;

        /// <summary>
        /// Maximal number of items which are visible at once (without scrolling)
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="value"/> is less than 1.
        /// </exception>
        public int MaxVisibleItemCount
        {
            get => _maxVisibleItemCount;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _maxVisibleItemCount = value;
                SetFormSize();
            }
        }

        /// <summary>
        /// Default selected index set whenever the suggestion form is shown.
        /// </summary>
        public int DefaultSelectedIndex { get; set; } = 0;

        private int _selectedIndex = -1;

        /// <summary>
        /// Index of the selected item
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                var oldSelectedIndex = _selectedIndex;

                // change current selected index
                if (value < 0 || value >= _items.Count)
                {
                    _selectedIndex = -1;
                }
                else
                {
                    _selectedIndex = value;
                    _suggestionControl.EnsureVisible(_selectedIndex);
                }

                // redraw the 2 changed elements
                _suggestionControl.Invalidate(oldSelectedIndex);
                _suggestionControl.Invalidate(_selectedIndex);

                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private Color _selectedBackColor = Color.FromArgb(unchecked((int)0xFF007acc));

        /// <summary>
        /// Background color of the selected item.
        /// </summary>
        public Color SelectedBackColor
        {
            get => _selectedBackColor;
            set
            {
                _selectedBackColor = value;
                _suggestionControl.Invalidate();
            }
        }

        private Color _selectedForeColor = Color.White;

        /// <summary>
        /// Text color of the selected item.
        /// </summary>
        public Color SelectedForeColor
        {
            get => _selectedForeColor;
            set
            {
                _selectedForeColor = value;
                _suggestionControl.Invalidate();
            }
        }

        private Color _metadataForeColor = Color.Gray;

        /// <summary>
        /// Text color of the metadata string
        /// </summary>
        public Color MetadataForeColor
        {
            get => _metadataForeColor;
            set
            {
                _metadataForeColor = value;
                _suggestionControl.Invalidate();
            }
        }

        /// <summary>
        /// Event occurs whenever user accepts a suggestion
        /// </summary>
        public event EventHandler<SuggestionEventArgs> Accepted;

        /// <summary>
        /// Event occurs whenever <see cref="SelectedIndex"/> changes
        /// </summary>
        public event EventHandler SelectedIndexChanged;

        /// <summary>
        /// Event occurs whenever <see cref="Items"/> changes.
        /// </summary>
        public event EventHandler ItemsChanged;

        private Control _currentControl;

        public SuggestionView() : this(null)
        {
        }

        public SuggestionView(Control control)
        {
            SetStyle(ControlStyles.DoubleBuffer, true);

            InitializeComponent();
            Hide();

            _suggestionControl = new SuggestionControl(this)
            {
                Location = new Point(1, 1),
                Size = new Size(ClientSize.Width - 2, ClientSize.Height - 2)
            };
            _suggestionControl.MouseDown += SuggestionControl_MouseDown;
            Controls.Add(_suggestionControl);

            AttachTo(control);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">
        /// true if managed resources should be disposed; otherwise, false.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DetachControl();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Called whenever the items collection changes
        /// </summary>
        protected virtual void OnItemsChanged()
        {
            if (_items.Count <= 0)
            {
                Hide();
            }
            else
            {
                SetFormSize();
            }

            _suggestionControl.UpdateClientSize();
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Show suggestions at current control. If no control is attached (see
        /// <see cref="AttachTo"/>), this will be no-op.
        /// </summary>
        public void ShowAtCurrentControl()
        {
            if (_items.Count <= 0 || _currentControl == null)
            {
                return;
            }

            SelectedIndex = DefaultSelectedIndex;
            SetLocation();
            Show();
        }

        /// <summary>
        /// Set <paramref name="control"/> as current control of this suggestion form.
        /// This is a no-op of <paramref name="control"/> is current control.
        /// </summary>
        /// <param name="control"></param>
        public void AttachTo(Control control)
        {
            if (_currentControl != control)
            {
                DetachControl();
                _currentControl = control;
                RegisterControlEventHandlers(_currentControl);
            }
        }

        /// <summary>
        /// Remove all event handlers added to _currentControl and hide this form.
        /// </summary>
        public void DetachControl()
        {
            if (_currentControl == null)
            {
                return;
            }

            UnregisterControlEventHandlers(_currentControl);
            _currentControl = null;
            Hide();
        }

        /// <summary>
        /// Set correct location of this suggestion form.
        /// </summary>
        private void SetLocation()
        {
            if (_currentControl == null)
            {
                return;
            }

            var location = _currentControl.PointToScreen(new Point(0, 0));
            Location = new Point(location.X, location.Y + _currentControl.Height);
            FixLocation();
        }

        /// <summary>
        /// Set form size so that there are at most <see cref="MaxVisibleItemCount"/> items
        /// visible at a time. If there is less items than that, the size will be adjusted to
        /// fit them all.
        /// </summary>
        private void SetFormSize()
        {
            const int border = 2;
            int itemHeight = _suggestionControl.MeasureItemHeight();
            var visibleItemCount = Math.Min(_items.Count, MaxVisibleItemCount);
            Size = new Size(Width, visibleItemCount * itemHeight + border);
            FixLocation();
        }

        /// <summary>
        /// Fix form location so that it is fully visible on its screen
        /// </summary>
        private void FixLocation()
        {
            if (_currentControl == null)
            {
                return;
            }

            var screen = Screen.FromControl(_currentControl);
            var bounds = new Rectangle(PointToScreen(Point.Empty), Size);
            var correctedBounds = bounds.EnsureInside(screen.Bounds);
            Location = correctedBounds.Location;
        }

        /// <summary>
        /// Accept a suggestion which is currently selected
        /// </summary>
        private void AcceptSelectedSuggestion()
        {
            Hide();
            if (SelectedIndex < 0)
            {
                return;
            }

            var item = _items[SelectedIndex];
            Accepted?.Invoke(this, new SuggestionEventArgs(item));
        }

        #region Event Handlers

        private void RegisterControlEventHandlers(Control control)
        {
            control.KeyDown += CurrentControl_KeyDown;
            control.Disposed += CurrentControl_Dispsed;
            control.VisibleChanged += CurrentControl_VisibleChanged;
        }

        private void UnregisterControlEventHandlers(Control control)
        {
            control.KeyDown -= CurrentControl_KeyDown;
            control.Disposed -= CurrentControl_Dispsed;
            control.VisibleChanged -= CurrentControl_VisibleChanged;
        }

        private void CurrentControl_VisibleChanged(object sender, EventArgs e)
        {
            Hide();
        }

        private void CurrentControl_Dispsed(object sender, EventArgs e)
        {
            DetachControl();
        }

        private void CurrentControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Visible)
            {
                if (e.Control && e.KeyCode == Keys.Space)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ShowAtCurrentControl();
                }
                return;
            }

            var selectedIndexDelta = 0;
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                Hide();
            }
            else if (e.KeyCode == Keys.Down)
            {
                e.Handled = true;
                selectedIndexDelta = 1;
            }
            else if (e.KeyCode == Keys.Up)
            {
                e.Handled = true;
                selectedIndexDelta = -1;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                e.SuppressKeyPress = true;
                AcceptSelectedSuggestion();
            }

            // change selected item
            if (_items.Count <= 0 || selectedIndexDelta == 0)
            {
                return;
            }

            var index = SelectedIndex + selectedIndexDelta;
            if (index < 0)
            {
                index = _items.Count - 1;
            }
            else if (index >= _items.Count)
            {
                index = index % _items.Count;
            }

            Trace.Assert(index >= 0);
            Trace.Assert(index < _items.Count);

            SelectedIndex = index;
        }
        
        private void SuggestionControl_MouseDown(object sender, MouseEventArgs e)
        {
            var location = PointToClient(MousePosition);
            var index = _suggestionControl.IndexFromPoint(location);
            if (index < 0)
            {
                return;
            }

            SelectedIndex = index;
            AcceptSelectedSuggestion();
        }

        #endregion

        #region Draw suggestions

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (_suggestionControl != null)
            {
                _suggestionControl.Size = new Size(ClientSize.Width - 2, ClientSize.Height - 2);
            }
        }

        private class SuggestionControl : UserControl
        {
            private readonly SuggestionView _suggestions;
            private readonly SolidBrush _selectedBrush;
            private readonly SolidBrush _selectedTextBrush;
            private readonly SolidBrush _mainTextBrush;
            private readonly SolidBrush _secondaryTextBrush;

            public SuggestionControl(SuggestionView suggestions)
            {
                SetStyle(ControlStyles.DoubleBuffer, true);

                _suggestions = suggestions;
                BackColor = Color.FromArgb(unchecked((int)0xFFeeeef2));
                AutoScroll = true;

                _selectedBrush = new SolidBrush(_suggestions.SelectedBackColor);
                _selectedTextBrush = new SolidBrush(_suggestions.SelectedForeColor);
                _mainTextBrush = new SolidBrush(ForeColor);
                _secondaryTextBrush = new SolidBrush(_suggestions.MetadataForeColor);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _selectedBrush?.Dispose();
                    _selectedTextBrush?.Dispose();
                    _mainTextBrush?.Dispose();
                    _secondaryTextBrush?.Dispose();
                }
                base.Dispose(disposing);
            }

            /// <summary>
            /// Measure current item height
            /// </summary>
            /// <returns></returns>
            public int MeasureItemHeight()
            {
                return (int) (Font.Height * 1.7);
            }

            /// <summary>
            /// Make sure item with index <paramref name="itemIndex"/> is visible.
            /// </summary>
            /// <remarks>
            /// If <paramref name="itemIndex"/> is out of range, this will be no-op.
            /// </remarks>
            /// <param name="itemIndex">Index of an item to show</param>
            public void EnsureVisible(int itemIndex)
            {
                if (itemIndex < 0 || itemIndex >= _suggestions._items.Count)
                {
                    return;
                }

                var scrollStart = -AutoScrollPosition.Y;
                var scrollEnd = scrollStart + Height;

                var itemBounds = GetItemBounds(itemIndex);
                var itemStart = itemBounds.Y;
                var itemEnd = itemStart + itemBounds.Height;

                var scrollDelta = itemEnd - scrollEnd;
                if (scrollDelta < 0)
                {
                    scrollDelta = Math.Min(itemStart - scrollStart, 0);
                }

                AutoScrollPosition = new Point(
                    AutoScrollPosition.X,
                    scrollStart + scrollDelta);
            }

            private Point LocalToScroll(Point localLocation)
            {
                return new Point(
                    localLocation.X - AutoScrollPosition.X,
                    localLocation.Y - AutoScrollPosition.Y
                );
            }

            private Point ScrollToLocal(Point scrollLocation)
            {
                return new Point(
                    scrollLocation.X + AutoScrollPosition.X,
                    scrollLocation.Y + AutoScrollPosition.Y
                );
            }

            private Rectangle GetItemBounds(int itemIndex)
            {
                var itemHeight = MeasureItemHeight();
                return new Rectangle(
                    new Point(0, itemIndex * itemHeight),
                    new Size(ClientSize.Width, itemHeight));
            }

            /// <summary>
            /// Get item at <paramref name="location"/>
            /// </summary>
            /// <param name="location">Queried location in local coordinates</param>
            /// <returns>
            /// Index of an item at <paramref name="location"/> or -1 if there is none.
            /// </returns>
            public int IndexFromPoint(Point location)
            {
                var scrollLocation = LocalToScroll(location);
                if (scrollLocation.Y < 0)
                {
                    return -1;
                }

                var itemHeight = MeasureItemHeight();
                var index = scrollLocation.Y / itemHeight;
                if (index >= _suggestions._items.Count)
                {
                    return -1;
                }

                return index;
            }

            /// <summary>
            /// Update the size of the scrollable area.
            /// </summary>
            public void UpdateClientSize()
            {
                AutoScrollMinSize = new Size(0, MeasureItemHeight() * _suggestions._items.Count);
            }

            /// <summary>
            /// Invalidate item with index <paramref name="itemIndex"/>. If there is no item
            /// which such index, this will be no-op.
            /// </summary>
            /// <param name="itemIndex">Index of an item to invalidate</param>
            public void Invalidate(int itemIndex)
            {
                if (itemIndex < 0 || itemIndex >= _suggestions._items.Count)
                {
                    return;
                }

                var bounds = GetItemBounds(itemIndex);
                bounds.Location = ScrollToLocal(bounds.Location);
                Invalidate(bounds);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
            
                // draw items
                foreach (var i in GetVisibleItems())
                {
                    var item = _suggestions._items[i];
                    var itemBounds = GetItemBounds(i);
                    itemBounds.Location = ScrollToLocal(itemBounds.Location);

                    // draw selected node background
                    var primaryTextBrush = _mainTextBrush;
                    var secondaryTextBrush = _secondaryTextBrush;
                    if (_suggestions.SelectedIndex == i)
                    {
                        e.Graphics.FillRectangle(_selectedBrush, itemBounds);
                        primaryTextBrush = _selectedTextBrush;
                        secondaryTextBrush = _selectedTextBrush;
                    }

                    // draw text
                    SizeF metadataTextSize = e.Graphics.MeasureString(item.Category, Font);

                    var mainTextBounds = new Rectangle(
                        itemBounds.X + Font.Height / 2,
                        itemBounds.Y + itemBounds.Height / 2 - Font.Height / 2,
                        ClientSize.Width - (int) metadataTextSize.Width - Font.Height / 2,
                        Font.Height
                    );

                    e.Graphics.DrawString(
                        item.Text,
                        Font,
                        primaryTextBrush,
                        mainTextBounds);

                    e.Graphics.DrawString(
                        item.Category,
                        Font,
                        secondaryTextBrush,
                        itemBounds.Width - metadataTextSize.Width - Font.Height / 2,
                        itemBounds.Y + itemBounds.Height / 2 - Font.Height / 2);
                }
            }
            
            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                UpdateClientSize();
            }

            private IEnumerable<int> GetVisibleItems()
            {
                var itemHeight = MeasureItemHeight();
                var bounds = new Rectangle(LocalToScroll(Point.Empty), ClientSize);
                var firstIndex = bounds.Y / itemHeight;
                var lastIndex = Math.Min(
                    (bounds.Y + bounds.Height) / itemHeight + 1, 
                    _suggestions._items.Count);
                for (; firstIndex < lastIndex; ++firstIndex)
                {
                    yield return firstIndex;
                }
            }
        }
        
        #endregion

        // Make sure the form is topmost but it does not steal focus from other controls

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams baseParams = base.CreateParams;

                const int wsExNoactivate = 0x08000000;
                const int wsExToolwindow = 0x00000080;
                const int wsExTopmost = 0x00000008;
                baseParams.ExStyle |= wsExNoactivate | wsExToolwindow | wsExTopmost;

                return baseParams;
            }
        }
    }
}
