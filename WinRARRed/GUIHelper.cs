using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace WinRARRed
{
    public static class GUIHelper
    {
        public static bool BrowseOpenFile(TextBox textBox, string title, string filter = "All Files (*.*)|*.*")
        {
            using OpenFileDialog dialog = new()
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = false,
                FileName = !string.IsNullOrEmpty(textBox.Text) ? Path.GetFileName(textBox.Text) : string.Empty
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = dialog.FileName;
                return true;
            }

            return false;
        }

        public static string? BrowseOpenFile(string title, string filter = "All Files (*.*)|*.*")
        {
            using OpenFileDialog dialog = new()
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = false
            };

            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        }

        public static bool BrowseSaveFile(TextBox textBox, string title, string filter = "All Files (*.*)|*.*")
        {
            using SaveFileDialog dialog = new()
            {
                Title = title,
                Filter = filter,
                FileName = !string.IsNullOrEmpty(textBox.Text) ? Path.GetFileName(textBox.Text) : string.Empty
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = dialog.FileName;
                return true;
            }

            return false;
        }

        public static string? BrowseSaveFile(string title, string filter = "All Files (*.*)|*.*")
        {
            using SaveFileDialog dialog = new()
            {
                Title = title,
                Filter = filter
            };

            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        }

        public static bool BrowseOpenDirectory(TextBox textBox, string title)
        {
            using FolderBrowserDialog dialog = new()
            {
                Description = title,
                SelectedPath = textBox.Text
            };

            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                textBox.Text = dialog.SelectedPath;
                return true;
            }

            return false;
        }


        public static void ShowError(Control? control, string text, string title = "Error")
        {
            if (control != null)
            {
                SetFocus(control);
            }

            if (!string.IsNullOrEmpty(text))
            {
                MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool ShowWarning(Control? control, string text, string title = "Warning")
        {
            if (control != null)
            {
                SetFocus(control);
            }

            return !string.IsNullOrEmpty(text)
                && MessageBox.Show(text, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        }

        public static void ShowInfo(Control? control, string text, string title = "Information")
        {
             if (control != null)
            {
                SetFocus(control);
            }

            if (!string.IsNullOrEmpty(text))
            {
                MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public static void SetFocus(Control control)
        {
            Stack<Control> parents = new();
            Control? parent = control;
            do
            {
                parents.Push(parent);
            } while ((parent = parent.Parent) != null);

            TabControl? parentTabControl = null;
            do
            {
                parent = parents.Pop();
                if (parent is TabControl tabControl)
                {
                    parentTabControl = tabControl;
                }
                else if (parentTabControl != null && parent is TabPage tabPage)
                {
                    parentTabControl.SelectedTab = tabPage;
                    parentTabControl = null;
                }

                parent.Focus();
            }
            while (parents.Count != 0);
        }
    }
}
