using System;

namespace MediaBrowser.Models
{
    public enum MediaType
    {
        Image,
        Video,
        Animated,
        Unknown
    }

    public class MediaFile
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public MediaType Type { get; set; }
        public string Extension { get; set; } = string.Empty;

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
    }
}
