using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediaBrowser.Models
{
    /// <summary>Represents one immediate subdirectory of the scanned root, shown as a checkbox in the left panel.</summary>
    public class SubDirectoryNode : INotifyPropertyChanged
    {
        private bool _isChecked = true;

        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
