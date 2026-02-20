using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace MediaBrowser.Services
{
    public static class ShellHelper
    {
        // ── Public API ────────────────────────────────────────────────────────

        public static void Open(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        public static void OpenWith(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo("rundll32.exe",
                    $"shell32.dll,OpenAs_RunDLL {path}")
                { UseShellExecute = true });
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        public static void CopyToClipboard(string path)
        {
            var sc = new StringCollection { path };
            Clipboard.SetFileDropList(sc);
        }

        public static void CutToClipboard(string path)
        {
            var sc = new StringCollection { path };
            var data = new DataObject();
            data.SetFileDropList(sc);
            // Signal cut (DROPEFFECT_MOVE = 2)
            var dropEffect = new byte[] { 2, 0, 0, 0 };
            data.SetData("Preferred DropEffect", new MemoryStream(dropEffect));
            Clipboard.SetDataObject(data, true);
        }

        /// <summary>Sends the file to the Recycle Bin with a confirmation prompt.
        /// Returns true if the file was deleted.</summary>
        public static bool DeleteFile(string path)
        {
            var result = MessageBox.Show(
                $"Move \"{Path.GetFileName(path)}\" to the Recycle Bin?",
                "Delete File",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return false;

            var fo = new SHFILEOPSTRUCT
            {
                hwnd = IntPtr.Zero,
                wFunc = 3,                     // FO_DELETE
                pFrom = path + '\0' + '\0',
                fFlags = 64                    // FOF_ALLOWUNDO = send to Recycle Bin
            };
            int code = SHFileOperationW(ref fo);
            if (code != 0)
                ShowError($"Delete failed (code {code}).");
            return code == 0;
        }

        /// <summary>Shows an inline rename dialog and renames the file.
        /// Returns the new full path on success, or null if cancelled / failed.</summary>
        public static string? RenameFile(string path, Window owner)
        {
            var current = Path.GetFileName(path);
            var newName = ShowInputDialog("Rename", "Enter new filename:", current, owner);
            if (string.IsNullOrWhiteSpace(newName) || newName == current) return null;

            var newPath = Path.Combine(Path.GetDirectoryName(path)!, newName);
            try
            {
                File.Move(path, newPath);
                return newPath;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                return null;
            }
        }

        /// <summary>Shows the Windows file-properties dialog.</summary>
        public static void ShowProperties(string path)
        {
            try
            {
                var info = new SHELLEXECUTEINFO
                {
                    cbSize = Marshal.SizeOf<SHELLEXECUTEINFO>(),
                    fMask = 0x0000000C,    // SEE_MASK_INVOKEIDLIST
                    lpVerb = "properties",
                    lpFile = path,
                    nShow = 1
                };
                ShellExecuteEx(ref info);
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void ShowError(string message)
            => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>Creates a small themed modal dialog with a single TextBox.</summary>
        public static string? ShowInputDialog(string title, string prompt, string defaultValue, Window? owner)
        {
            var dlg = new Window
            {
                Title = title,
                Width = 420,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Background = GetBrush("SurfaceBrush")
            };

            string? result = null;

            var promptLabel = new System.Windows.Controls.TextBlock
            {
                Text = prompt,
                Margin = new Thickness(12, 10, 12, 2),
                Foreground = GetBrush("SubtextBrush"),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12
            };

            var txt = new System.Windows.Controls.TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(12, 12, 12, 8),
                Background = GetBrush("BackgroundBrush"),
                Foreground = GetBrush("TextBrush"),
                BorderBrush = GetBrush("BorderBrush"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 13,
                CaretBrush = GetBrush("TextBrush")
            };

            var btnOk = new System.Windows.Controls.Button
            {
                Content = "OK",
                Padding = new Thickness(20, 6, 20, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Background = GetBrush("PrimaryBrush"),
                Foreground = GetBrush("TextBrush"),
                BorderThickness = new Thickness(0),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Padding = new Thickness(20, 6, 20, 6),
                Background = GetBrush("HoverBrush"),
                Foreground = GetBrush("TextBrush"),
                BorderThickness = new Thickness(0),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            btnOk.Click += (_, _) => { result = txt.Text; dlg.DialogResult = true; };
            btnCancel.Click += (_, _) => { dlg.DialogResult = false; };

            txt.KeyDown += (_, ke) =>
            {
                if (ke.Key == System.Windows.Input.Key.Enter) { result = txt.Text; dlg.DialogResult = true; }
                else if (ke.Key == System.Windows.Input.Key.Escape) { dlg.DialogResult = false; }
            };

            var btnPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(12, 0, 12, 12)
            };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);

            var root = new System.Windows.Controls.StackPanel();
            root.Children.Add(promptLabel);
            root.Children.Add(txt);
            root.Children.Add(btnPanel);
            dlg.Content = root;

            dlg.Loaded += (_, _) => { txt.SelectAll(); txt.Focus(); };
            dlg.ShowDialog();
            return result;
        }

        private static System.Windows.Media.Brush GetBrush(string key)
        {
            if (Application.Current?.Resources[key] is System.Windows.Media.Brush brush)
                return brush;
            return System.Windows.Media.Brushes.Gray; // safe fallback
        }

        // ── P/Invoke ──────────────────────────────────────────────────────────

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperationW(ref SHFILEOPSTRUCT FileOp);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpVerb;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpParameters;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }
    }
}
