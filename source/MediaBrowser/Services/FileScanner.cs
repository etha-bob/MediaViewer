using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Models;

namespace MediaBrowser.Services
{
    public class FileScanner
    {
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".webp",
            ".psp", ".tga", ".pcx", ".xyz"
        };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mov", ".mkv", ".webm"
        };

        private static readonly HashSet<string> AnimatedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".gif", ".apng"
        };

        public static MediaType GetMediaType(string extension)
        {
            if (AnimatedExtensions.Contains(extension)) return MediaType.Animated;
            if (ImageExtensions.Contains(extension)) return MediaType.Image;
            if (VideoExtensions.Contains(extension)) return MediaType.Video;
            return MediaType.Unknown;
        }

        public static bool IsSupportedExtension(string extension)
        {
            return ImageExtensions.Contains(extension)
                || VideoExtensions.Contains(extension)
                || AnimatedExtensions.Contains(extension);
        }

        public static IEnumerable<string> GetAllSupportedExtensions()
        {
            foreach (var ext in ImageExtensions) yield return ext;
            foreach (var ext in VideoExtensions) yield return ext;
            foreach (var ext in AnimatedExtensions) yield return ext;
        }

        public async Task<List<MediaFile>> ScanDirectoryAsync(
            string rootPath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => ScanDirectory(rootPath, progress, cancellationToken), cancellationToken);
        }

        private List<MediaFile> ScanDirectory(
            string rootPath,
            IProgress<int>? progress,
            CancellationToken cancellationToken)
        {
            var results = new List<MediaFile>();
            int count = 0;

            try
            {
                var enumOptions = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.System
                };

                foreach (var filePath in Directory.EnumerateFiles(rootPath, "*.*", enumOptions))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var ext = Path.GetExtension(filePath);
                    if (!IsSupportedExtension(ext)) continue;

                    try
                    {
                        var info = new FileInfo(filePath);
                        results.Add(new MediaFile
                        {
                            Path = filePath,
                            Name = info.Name,
                            Size = info.Length,
                            DateCreated = info.CreationTime,
                            DateModified = info.LastWriteTime,
                            Type = GetMediaType(ext),
                            Extension = ext.TrimStart('.').ToUpperInvariant()
                        });
                    }
                    catch (IOException)
                    {
                        // Skip files that can't be accessed
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files without permission
                    }

                    count++;
                    if (count % 100 == 0)
                        progress?.Report(count);
                }
            }
            catch (DirectoryNotFoundException)
            {
                // Return empty list if directory doesn't exist
            }
            catch (UnauthorizedAccessException)
            {
                // Return partial results if access is denied at root
            }

            progress?.Report(count);
            return results;
        }
    }
}
