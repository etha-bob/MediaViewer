using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using MediaBrowser.ViewModels;
using MediaBrowser.Models;
using Microsoft.Win32;

namespace MediaBrowser
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            // Restore window size
            var config = Services.ConfigService.Load();
            if (config.WindowWidth > 0) Width = config.WindowWidth;
            if (config.WindowHeight > 0) Height = config.WindowHeight;

            // Hook up image loading for visible items
            _vm.DisplayedFiles.CollectionChanged += DisplayedFiles_CollectionChanged;

            // Keyboard shortcuts
            KeyDown += MainWindow_KeyDown;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Open last directory on startup
            if (!string.IsNullOrWhiteSpace(_vm.LastDirectory)
                && Directory.Exists(_vm.LastDirectory))
            {
                _ = _vm.ScanDirectoryAsync(_vm.LastDirectory);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _vm.CancelScan();
            _vm.SaveConfig();
            base.OnClosing(e);
        }

        // ── Browse ────────────────────────────────────────────────────────────
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Media Directory"
            };

            if (dialog.ShowDialog(this) == true)
            {
                await _vm.ScanDirectoryAsync(dialog.FolderName);
                LoadVisibleThumbnails();
            }
        }

        // ── Cancel ────────────────────────────────────────────────────────────
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.CancelScan();
        }

        // ── Sort direction toggle ─────────────────────────────────────────────
        private void SortDirButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.SortAscending = !_vm.SortAscending;
        }

        // ── Date filters ──────────────────────────────────────────────────────
        private void DateFromPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            _vm.FilterDateFrom = DateFromPicker.SelectedDate;
        }

        private void DateToPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            _vm.FilterDateTo = DateToPicker.SelectedDate;
        }

        // ── Size filters ──────────────────────────────────────────────────────
        private void SizeMinBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (long.TryParse(SizeMinBox.Text, out long kb))
                _vm.FilterSizeMin = kb * 1024;
            else
                _vm.FilterSizeMin = 0;
        }

        private void SizeMaxBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (long.TryParse(SizeMaxBox.Text, out long kb))
                _vm.FilterSizeMax = kb * 1024;
            else
                _vm.FilterSizeMax = long.MaxValue;
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

        // ── Keyboard shortcuts ────────────────────────────────────────────────
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemPlus && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                _vm.ThumbnailSize = Math.Min(800, _vm.ThumbnailSize + 50);
                e.Handled = true;
            }
            else if (e.Key == Key.OemMinus && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                _vm.ThumbnailSize = Math.Max(100, _vm.ThumbnailSize - 50);
                e.Handled = true;
            }
        }

        // ── Thumbnail loading ─────────────────────────────────────────────────
        private void DisplayedFiles_CollectionChanged(object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Defer thumbnail loading to after the UI updates
            Dispatcher.BeginInvoke(new Action(LoadVisibleThumbnails),
                System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Walks all Image controls in the ItemsControl and loads BitmapImage sources
        /// for items that are Image or Animated type. Uses DecodePixelWidth to keep
        /// memory usage proportional to the current thumbnail size.
        /// </summary>
        private void LoadVisibleThumbnails()
        {
            if (MediaItemsControl?.ItemsSource == null) return;

            foreach (var item in MediaItemsControl.Items)
            {
                if (item is not MediaFile mf) continue;
                if (mf.Type == MediaType.Video) continue;

                // Try to get the container from the ItemsControl
                var container = MediaItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container == null) continue;

                var img = FindChild<Image>(container);
                if (img == null) continue;

                // Skip if already loaded
                if (img.Source != null) continue;

                LoadImageAsync(img, mf.Path);
            }
        }

        private static void LoadImageAsync(Image img, string path)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 400; // Limit decode size for memory efficiency
                bmp.EndInit();
                bmp.Freeze();
                img.Source = bmp;
            }
            catch (Exception)
            {
                // Show broken image placeholder — leave Source as null
            }
        }

        /// <summary>Finds the first child of type T in a visual tree.</summary>
        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var found = FindChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
