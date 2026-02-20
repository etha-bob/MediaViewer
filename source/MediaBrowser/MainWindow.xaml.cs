using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MediaBrowser.Models;
using MediaBrowser.Services;
using MediaBrowser.ViewModels;
using Microsoft.Win32;

namespace MediaBrowser
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        // ConcurrentDictionary used as a thread-safe set: tracks paths currently being decoded
        private readonly ConcurrentDictionary<string, bool> _loadingSet
            = new(StringComparer.OrdinalIgnoreCase);

        // Debounce timer for scroll-triggered loading
        private readonly DispatcherTimer _scrollTimer;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            // Restore window size
            var config = ConfigService.Load();
            if (config.WindowWidth > 0) Width = config.WindowWidth;
            if (config.WindowHeight > 0) Height = config.WindowHeight;

            // Apply persisted panel widths
            ApplyPanelState();

            // When the displayed collection resets, clear loading state and re-trigger
            _vm.DisplayedFiles.CollectionChanged += (_, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                    _loadingSet.Clear(); // ConcurrentDictionary.Clear() is thread-safe
                Dispatcher.InvokeAsync(LoadVisibleMedia, DispatcherPriority.Background);
            };

            // Scroll debounce: 80ms after last scroll event
            _scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _scrollTimer.Tick += (_, _) => { _scrollTimer.Stop(); LoadVisibleMedia(); };

            // Keyboard shortcuts
            KeyDown += MainWindow_KeyDown;

            // Property change: when SelectedFile changes update the detail preview thumbnail
            _vm.PropertyChanged += Vm_PropertyChanged;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (!string.IsNullOrWhiteSpace(_vm.LastDirectory)
                && Directory.Exists(_vm.LastDirectory))
                _ = _vm.ScanDirectoryAsync(_vm.LastDirectory);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _vm.CancelScan();
            _vm.SaveConfig();
            base.OnClosing(e);
        }

        // ── Panel state ───────────────────────────────────────────────────────
        private void ApplyPanelState()
        {
            LeftPanelCol.Width = _vm.LeftPanelOpen ? new GridLength(220) : new GridLength(0);
            RightPanelCol.Width = _vm.RightPanelOpen ? new GridLength(280) : new GridLength(0);
        }

        private void LeftPanelToggle_Click(object sender, RoutedEventArgs e)
        {
            _vm.LeftPanelOpen = LeftPanelToggle.IsChecked == true;
            LeftPanelCol.Width = _vm.LeftPanelOpen ? new GridLength(220) : new GridLength(0);
        }

        private void RightPanelToggle_Click(object sender, RoutedEventArgs e)
        {
            _vm.RightPanelOpen = RightPanelToggle.IsChecked == true;
            RightPanelCol.Width = _vm.RightPanelOpen ? new GridLength(280) : new GridLength(0);
        }

        // ── Browse ────────────────────────────────────────────────────────────
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Select Media Directory" };
            if (dialog.ShowDialog(this) == true)
                await _vm.ScanDirectoryAsync(dialog.FolderName);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => _vm.CancelScan();

        // ── Sort ──────────────────────────────────────────────────────────────
        private void SortDirButton_Click(object sender, RoutedEventArgs e)
            => _vm.SortAscending = !_vm.SortAscending;

        // ── Date filters ──────────────────────────────────────────────────────
        private void DateFromPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
            => _vm.FilterDateFrom = DateFromPicker.SelectedDate;

        private void DateToPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
            => _vm.FilterDateTo = DateToPicker.SelectedDate;

        // ── Size filters ──────────────────────────────────────────────────────
        private void SizeMinBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.FilterSizeMin = long.TryParse(SizeMinBox.Text, out long kb) ? kb * 1024 : 0;
        }

        private void SizeMaxBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.FilterSizeMax = long.TryParse(SizeMaxBox.Text, out long kb) ? kb * 1024 : long.MaxValue;
        }

        // ── Clear filters ─────────────────────────────────────────────────────
        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            FilterTypeCombo.SelectedIndex = 0;
            SortFieldCombo.SelectedIndex = 0;
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            SizeMinBox.Text = string.Empty;
            SizeMaxBox.Text = string.Empty;
            _vm.FilterSizeMin = 0;
            _vm.FilterSizeMax = long.MaxValue;
        }

        // ── Subdirectory panel ────────────────────────────────────────────────
        private void SubDirSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var n in _vm.SubDirectories) n.IsChecked = true;
        }

        private void SubDirSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var n in _vm.SubDirectories) n.IsChecked = false;
        }

        // ── Media card click/double-click ─────────────────────────────────────
        private void MediaCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (sender is not FrameworkElement card) return;
            var mf = card.DataContext as MediaFile;
            if (mf == null) return;

            if (e.ClickCount == 2)
            {
                ShellHelper.Open(mf.Path);
                e.Handled = true;
            }
            else
            {
                _vm.SelectedFile = mf;
                // Auto-open right panel on first selection
                if (!_vm.RightPanelOpen)
                {
                    _vm.RightPanelOpen = true;
                    RightPanelToggle.IsChecked = true;
                    RightPanelCol.Width = new GridLength(280);
                }
                LoadDetailThumbnail(mf);
                _ = LoadDimensionsAsync(mf);
            }
        }

        // ── Right-panel detail thumbnail + dimensions ─────────────────────────
        private void LoadDetailThumbnail(MediaFile mf)
        {
            if (DetailThumbnail == null) return;
            if (mf.Type != MediaType.Image && mf.Type != MediaType.Animated)
            {
                DetailThumbnail.Source = null;
                return;
            }
            _ = Task.Run(() =>
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(mf.Path);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 280;
                    bmp.EndInit();
                    bmp.Freeze();
                    Dispatcher.Invoke(() => DetailThumbnail.Source = bmp);
                }
                catch { }
            });
        }

        private static async Task LoadDimensionsAsync(MediaFile mf)
        {
            if (mf.Type != MediaType.Image && mf.Type != MediaType.Animated) return;
            if (mf.PixelWidth > 0) return; // already loaded
            try
            {
                var (w, h) = await Task.Run(() =>
                {
                    using var stream = File.OpenRead(mf.Path);
                    var dec = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    return (dec.Frames[0].PixelWidth, dec.Frames[0].PixelHeight);
                });
                mf.PixelWidth = w;
                mf.PixelHeight = h;
            }
            catch { }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_vm.SelectedFile) && _vm.SelectedFile != null)
            {
                LoadDetailThumbnail(_vm.SelectedFile);
                _ = LoadDimensionsAsync(_vm.SelectedFile);
            }
        }

        private void DetailOpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedFile != null)
                ShellHelper.Open(_vm.SelectedFile.Path);
        }

        // ── Context menu helpers ──────────────────────────────────────────────
        private static MediaFile? GetMenuFile(object sender)
        {
            if (sender is MenuItem mi
                && mi.Parent is ContextMenu cm
                && cm.PlacementTarget is FrameworkElement el)
                return el.DataContext as MediaFile;
            return null;
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (GetMenuFile(sender) is MediaFile mf) ShellHelper.Open(mf.Path);
        }

        private void MenuOpenWith_Click(object sender, RoutedEventArgs e)
        {
            if (GetMenuFile(sender) is MediaFile mf) ShellHelper.OpenWith(mf.Path);
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            if (GetMenuFile(sender) is MediaFile mf) ShellHelper.CopyToClipboard(mf.Path);
        }

        private void MenuCut_Click(object sender, RoutedEventArgs e)
        {
            if (GetMenuFile(sender) is MediaFile mf) ShellHelper.CutToClipboard(mf.Path);
        }

        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (GetMenuFile(sender) is not MediaFile mf) return;
            if (ShellHelper.DeleteFile(mf.Path))
                _vm.ApplyFilterAndSort(); // refresh the grid after deletion
        }

        private void MenuRename_Click(object sender, RoutedEventArgs e)
        {
            if (GetMenuFile(sender) is not MediaFile mf) return;
            var newPath = ShellHelper.RenameFile(mf.Path, this);
            if (newPath != null)
            {
                mf.Path = newPath;
                mf.Name = System.IO.Path.GetFileName(newPath);
                mf.Directory = System.IO.Path.GetDirectoryName(newPath) ?? mf.Directory;
            }
        }

        private void MenuProperties_Click(object sender, RoutedEventArgs e)
        {
            if (GetMenuFile(sender) is MediaFile mf) ShellHelper.ShowProperties(mf.Path);
        }

        // ── Keyboard shortcuts ────────────────────────────────────────────────
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add)
                {
                    _vm.ThumbnailSize = Math.Min(800, _vm.ThumbnailSize + 50);
                    e.Handled = true;
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                {
                    _vm.ThumbnailSize = Math.Max(100, _vm.ThumbnailSize - 50);
                    e.Handled = true;
                }
            }
        }

        // ── Scroll-driven lazy thumbnail loading ──────────────────────────────
        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            _scrollTimer.Stop();
            _scrollTimer.Start();
        }

        /// <summary>
        /// Iterates all item containers, checks if they're inside the viewport
        /// (plus a one-screen buffer for smooth scrolling), and:
        ///   - Starts async image decoding for images/GIFs that are in view but not loaded.
        ///   - Sets the Source + first-frame-pause for videos that are in view.
        ///   - Frees the source for items that are well outside the viewport.
        /// All BitmapImage decoding happens on a background thread; only the
        /// final assignment to Image.Source touches the UI thread.
        /// </summary>
        private void LoadVisibleMedia()
        {
            if (MainScrollViewer == null || MediaItemsControl == null) return;

            double viewTop = MainScrollViewer.VerticalOffset;
            double viewBottom = viewTop + MainScrollViewer.ViewportHeight;
            double buffer = MainScrollViewer.ViewportHeight; // preload one screen ahead/behind

            foreach (var item in MediaItemsControl.Items)
            {
                if (item is not MediaFile mf) continue;

                var container = MediaItemsControl.ItemContainerGenerator
                    .ContainerFromItem(item) as FrameworkElement;
                if (container == null || !container.IsLoaded) continue;

                // Get container bounds relative to the ScrollViewer
                Point topLeft;
                try
                {
                    topLeft = container.TransformToAncestor(MainScrollViewer).Transform(new Point(0, 0));
                }
                catch { continue; }

                double itemTop = topLeft.Y;
                double itemBottom = itemTop + container.ActualHeight;

                bool inRange = itemBottom > (viewTop - buffer) && itemTop < (viewBottom + buffer);

                if (mf.Type == MediaType.Video)
                {
                    var me = FindChild<MediaElement>(container);
                    var overlay = FindChildByName<Grid>(container, "VideoPlaceholder");
                    if (me == null) continue;

                    if (inRange && me.Source == null)
                    {
                        me.MediaOpened += VideoElement_MediaOpened;
                        me.Source = new Uri(mf.Path);
                        me.Play();
                        if (overlay != null) overlay.Visibility = Visibility.Collapsed;
                    }
                    else if (!inRange && me.Source != null)
                    {
                        me.Stop();
                        me.Source = null;
                        if (overlay != null) overlay.Visibility = Visibility.Visible;
                    }
                }
                else if (mf.Type == MediaType.Image || mf.Type == MediaType.Animated)
                {
                    var img = FindChild<Image>(container);
                    if (img == null) continue;

                    if (inRange && img.Source == null && !_loadingSet.ContainsKey(mf.Path))
                    {
                        _loadingSet[mf.Path] = true;
                        _ = LoadImageAsync(img, mf.Path);
                    }
                    else if (!inRange && img.Source != null)
                    {
                        img.Source = null;
                        _loadingSet.TryRemove(mf.Path, out _);
                    }
                }
            }
        }

        private async Task LoadImageAsync(Image img, string path)
        {
            try
            {
                var decodeWidth = Math.Max(200, _vm.ThumbnailSize);
                var bmp = await Task.Run(() =>
                {
                    var b = new BitmapImage();
                    b.BeginInit();
                    b.UriSource = new Uri(path);
                    b.CacheOption = BitmapCacheOption.OnLoad;
                    b.DecodePixelWidth = decodeWidth;
                    b.EndInit();
                    b.Freeze();
                    return b;
                });

                // Check if the path is still in the loading set (not been cleared by a scroll-away)
                if (_loadingSet.ContainsKey(path))
                    img.Source = bmp;
            }
            catch
            {
                _loadingSet.TryRemove(path, out _);
            }
        }

        // ── Video hover play/pause ────────────────────────────────────────────
        private static void VideoElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (sender is MediaElement me)
            {
                me.MediaOpened -= VideoElement_MediaOpened;
                me.Pause();
                me.Position = TimeSpan.FromMilliseconds(250);
            }
        }

        private void VideoElement_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is MediaElement me && me.Source != null) me.Play();
        }

        private void VideoElement_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is MediaElement me && me.Source != null)
            {
                me.Pause();
                me.Position = TimeSpan.FromMilliseconds(250);
            }
        }

        // ── Visual tree helpers ───────────────────────────────────────────────
        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var found = FindChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        private static T? FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T fe && fe.Name == name) return fe;
                var found = FindChildByName<T>(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
