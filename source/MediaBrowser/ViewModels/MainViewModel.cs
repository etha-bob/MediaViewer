using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Models;
using MediaBrowser.Services;

namespace MediaBrowser.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly FileScanner _scanner = new();
        private AppConfig _config;
        private List<MediaFile> _allFiles = new();
        private CancellationTokenSource? _scanCts;

        // ── Bindable Collections ──────────────────────────────────────────────
        public ObservableCollection<MediaFile> DisplayedFiles { get; } = new();
        public ObservableCollection<SubDirectoryNode> SubDirectories { get; } = new();

        // ── Scanning state ────────────────────────────────────────────────────
        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set { _isScanning = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsIdle)); }
        }

        public bool IsIdle => !_isScanning;

        private int _scannedCount;
        public int ScannedCount
        {
            get => _scannedCount;
            set { _scannedCount = value; OnPropertyChanged(); }
        }

        private string _statusText = "Ready. Click 'Browse' to open a directory.";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        // ── Zoom ──────────────────────────────────────────────────────────────
        private int _thumbnailSize;
        public int ThumbnailSize
        {
            get => _thumbnailSize;
            set
            {
                if (_thumbnailSize == value) return;
                _thumbnailSize = value;
                OnPropertyChanged();
                _config.DefaultZoomLevel = value;
            }
        }

        // ── Aspect Ratio ──────────────────────────────────────────────────────
        private bool _preserveAspectRatio;
        public bool PreserveAspectRatio
        {
            get => _preserveAspectRatio;
            set
            {
                _preserveAspectRatio = value;
                OnPropertyChanged();
                _config.PreserveAspectRatio = value;
            }
        }

        // ── Selection ─────────────────────────────────────────────────────────
        private MediaFile? _selectedFile;
        public MediaFile? SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (_selectedFile != null) _selectedFile.IsSelected = false;
                _selectedFile = value;
                if (_selectedFile != null) _selectedFile.IsSelected = true;
                OnPropertyChanged();
            }
        }

        // ── Panel visibility ──────────────────────────────────────────────────
        private bool _leftPanelOpen;
        public bool LeftPanelOpen
        {
            get => _leftPanelOpen;
            set { _leftPanelOpen = value; OnPropertyChanged(); _config.LeftPanelOpen = value; }
        }

        private bool _rightPanelOpen;
        public bool RightPanelOpen
        {
            get => _rightPanelOpen;
            set { _rightPanelOpen = value; OnPropertyChanged(); _config.RightPanelOpen = value; }
        }

        // ── Filter / Sort properties ──────────────────────────────────────────
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilterAndSort(); }
        }

        private string _filterType = "All";
        public string FilterType
        {
            get => _filterType;
            set { _filterType = value; OnPropertyChanged(); ApplyFilterAndSort(); _config.LastFilterType = value; }
        }

        private string _sortField = "Name";
        public string SortField
        {
            get => _sortField;
            set { _sortField = value; OnPropertyChanged(); ApplyFilterAndSort(); _config.LastSortField = value; }
        }

        private bool _sortAscending = true;
        public bool SortAscending
        {
            get => _sortAscending;
            set { _sortAscending = value; OnPropertyChanged(); ApplyFilterAndSort(); _config.LastSortAscending = value; }
        }

        private DateTime? _filterDateFrom;
        public DateTime? FilterDateFrom
        {
            get => _filterDateFrom;
            set { _filterDateFrom = value; OnPropertyChanged(); ApplyFilterAndSort(); }
        }

        private DateTime? _filterDateTo;
        public DateTime? FilterDateTo
        {
            get => _filterDateTo;
            set { _filterDateTo = value; OnPropertyChanged(); ApplyFilterAndSort(); }
        }

        private long _filterSizeMin;
        public long FilterSizeMin
        {
            get => _filterSizeMin;
            set { _filterSizeMin = value; OnPropertyChanged(); ApplyFilterAndSort(); }
        }

        private long _filterSizeMax = long.MaxValue;
        public long FilterSizeMax
        {
            get => _filterSizeMax;
            set { _filterSizeMax = value; OnPropertyChanged(); ApplyFilterAndSort(); }
        }

        private string _currentDirectory = string.Empty;
        public string CurrentDirectory
        {
            get => _currentDirectory;
            set { _currentDirectory = value; OnPropertyChanged(); }
        }

        // ── Filter type list ──────────────────────────────────────────────────
        public IEnumerable<string> FilterTypes { get; } = new[]
        {
            "All", "Images", "Videos", "Animated"
        };

        public IEnumerable<string> SortFields { get; } = new[]
        {
            "Name", "DateCreated", "DateModified", "Size", "Type"
        };

        // ── Constructor ───────────────────────────────────────────────────────
        public MainViewModel()
        {
            _config = ConfigService.Load();
            _thumbnailSize = _config.DefaultZoomLevel;
            _sortField = _config.LastSortField;
            _sortAscending = _config.LastSortAscending;
            _filterType = _config.LastFilterType;
            _preserveAspectRatio = _config.PreserveAspectRatio;
            _leftPanelOpen = _config.LeftPanelOpen;
            _rightPanelOpen = _config.RightPanelOpen;
        }

        // ── Scanning ──────────────────────────────────────────────────────────
        public async Task ScanDirectoryAsync(string path)
        {
            _scanCts?.Cancel();
            _scanCts = new CancellationTokenSource();
            var cts = _scanCts;

            IsScanning = true;
            ScannedCount = 0;
            _allFiles.Clear();
            DisplayedFiles.Clear();
            SubDirectories.Clear();
            SelectedFile = null;
            CurrentDirectory = path;
            StatusText = $"Scanning {path}…";
            _config.LastDirectory = path;

            var progress = new Progress<int>(n =>
            {
                ScannedCount = n;
                StatusText = $"Scanning… {n} files found";
            });

            try
            {
                var files = await _scanner.ScanDirectoryAsync(path, progress, cts.Token);
                _allFiles = files;

                // Build subdirectory list from scanned files
                var subDirs = files
                    .Select(f => f.SubDirectory)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(d => d)
                    .ToList();

                foreach (var dir in subDirs)
                {
                    var node = new SubDirectoryNode
                    {
                        Name = string.IsNullOrEmpty(dir) ? "(root)" : dir,
                        FullPath = string.IsNullOrEmpty(dir) ? path : Path.Combine(path, dir),
                        IsChecked = true
                    };
                    node.PropertyChanged += (_, _) => ApplyFilterAndSort();
                    SubDirectories.Add(node);
                }

                ApplyFilterAndSort();
                StatusText = $"Found {_allFiles.Count} media files in {path}";
            }
            catch (OperationCanceledException)
            {
                StatusText = "Scan cancelled.";
            }
            catch (Exception ex)
            {
                StatusText = $"Error scanning directory: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        public void CancelScan() => _scanCts?.Cancel();

        // ── Filter + Sort ─────────────────────────────────────────────────────
        public void ApplyFilterAndSort()
        {
            var query = _allFiles.AsEnumerable();

            // Subdirectory filter
            if (SubDirectories.Count > 0 && SubDirectories.Any(d => !d.IsChecked))
            {
                var checkedNames = SubDirectories
                    .Where(d => d.IsChecked)
                    .Select(d => d.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                query = query.Where(f =>
                {
                    var key = string.IsNullOrEmpty(f.SubDirectory) ? "(root)" : f.SubDirectory;
                    return checkedNames.Contains(key);
                });
            }

            // Type filter
            query = _filterType switch
            {
                "Images" => query.Where(f => f.Type == MediaType.Image),
                "Videos" => query.Where(f => f.Type == MediaType.Video),
                "Animated" => query.Where(f => f.Type == MediaType.Animated),
                _ => query
            };

            // Filename search
            if (!string.IsNullOrWhiteSpace(_searchText))
                query = query.Where(f => f.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            // Date range
            if (_filterDateFrom.HasValue)
                query = query.Where(f => f.DateModified >= _filterDateFrom.Value);
            if (_filterDateTo.HasValue)
                query = query.Where(f => f.DateModified <= _filterDateTo.Value.Date.AddDays(1).AddTicks(-1));

            // Size range
            if (_filterSizeMin > 0)
                query = query.Where(f => f.Size >= _filterSizeMin);
            if (_filterSizeMax < long.MaxValue)
                query = query.Where(f => f.Size <= _filterSizeMax);

            // Sort
            query = (_sortField, _sortAscending) switch
            {
                ("Name", true) => query.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase),
                ("Name", false) => query.OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase),
                ("DateCreated", true) => query.OrderBy(f => f.DateCreated),
                ("DateCreated", false) => query.OrderByDescending(f => f.DateCreated),
                ("DateModified", true) => query.OrderBy(f => f.DateModified),
                ("DateModified", false) => query.OrderByDescending(f => f.DateModified),
                ("Size", true) => query.OrderBy(f => f.Size),
                ("Size", false) => query.OrderByDescending(f => f.Size),
                ("Type", true) => query.OrderBy(f => f.Extension),
                ("Type", false) => query.OrderByDescending(f => f.Extension),
                _ => query.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            };

            DisplayedFiles.Clear();
            foreach (var f in query)
                DisplayedFiles.Add(f);

            StatusText = _allFiles.Count == 0
                ? "No media files loaded. Click 'Browse' to open a directory."
                : $"Showing {DisplayedFiles.Count} of {_allFiles.Count} files — {CurrentDirectory}";
        }

        // ── Persistence ───────────────────────────────────────────────────────
        public void SaveConfig() => ConfigService.Save(_config);

        public string LastDirectory => _config.LastDirectory;

        // ── INotifyPropertyChanged ────────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

