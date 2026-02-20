using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediaBrowser.Models
{
    public enum MediaType
    {
        Image,
        Video,
        Animated,
        Unknown
    }

    public class MediaFile : INotifyPropertyChanged
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        /// <summary>Immediate child subdirectory name under the scan root, or empty string for root-level files.</summary>
        public string SubDirectory { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public MediaType Type { get; set; }
        public string Extension { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private int _pixelWidth;
        public int PixelWidth
        {
            get => _pixelWidth;
            set { _pixelWidth = value; OnPropertyChanged(); OnPropertyChanged(nameof(DimensionDisplay)); }
        }

        private int _pixelHeight;
        public int PixelHeight
        {
            get => _pixelHeight;
            set { _pixelHeight = value; OnPropertyChanged(); OnPropertyChanged(nameof(DimensionDisplay)); }
        }

        public string DimensionDisplay => PixelWidth > 0 ? $"{PixelWidth} × {PixelHeight} px" : "—";

        public string SizeDisplay
        {
            get
            {
                if (Size < 1024) return $"{Size} B";
                if (Size < 1024 * 1024) return $"{Size / 1024.0:F1} KB";
                if (Size < 1024 * 1024 * 1024) return $"{Size / (1024.0 * 1024):F1} MB";
                return $"{Size / (1024.0 * 1024 * 1024):F2} GB";
            }
        }

        public string TypeDisplay => Type switch
        {
            MediaType.Image => "Image",
            MediaType.Video => "Video",
            MediaType.Animated => "Animated",
            _ => "Unknown"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
