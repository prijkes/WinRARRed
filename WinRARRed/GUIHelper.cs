using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace WinRARRed;

/// <summary>
/// Provides helper methods for common Windows Forms GUI operations such as file dialogs and message boxes.
/// </summary>
public static class GUIHelper
{
    /// <summary>
    /// Shows an open file dialog and updates the specified TextBox with the selected file path.
    /// </summary>
    /// <param name="textBox">The TextBox to update with the selected file path.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="filter">The file type filter (e.g., "RAR Files (*.rar)|*.rar").</param>
    /// <returns><c>true</c> if a file was selected; otherwise, <c>false</c>.</returns>
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

    /// <summary>
    /// Shows an open file dialog and returns the selected file path.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="filter">The file type filter.</param>
    /// <returns>The selected file path, or <c>null</c> if cancelled.</returns>
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

    /// <summary>
    /// Shows a save file dialog and updates the specified TextBox with the selected file path.
    /// </summary>
    /// <param name="textBox">The TextBox to update with the selected file path.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="filter">The file type filter.</param>
    /// <returns><c>true</c> if a file path was selected; otherwise, <c>false</c>.</returns>
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

    /// <summary>
    /// Shows a save file dialog and returns the selected file path.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="filter">The file type filter.</param>
    /// <returns>The selected file path, or <c>null</c> if cancelled.</returns>
    public static string? BrowseSaveFile(string title, string filter = "All Files (*.*)|*.*")
    {
        using SaveFileDialog dialog = new()
        {
            Title = title,
            Filter = filter
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    /// <summary>
    /// Shows a folder browser dialog and updates the specified TextBox with the selected directory path.
    /// </summary>
    /// <param name="textBox">The TextBox to update with the selected directory path.</param>
    /// <param name="title">The dialog description/title.</param>
    /// <returns><c>true</c> if a directory was selected; otherwise, <c>false</c>.</returns>
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


    /// <summary>
    /// Displays an error message box and optionally focuses the specified control.
    /// </summary>
    /// <param name="control">The control to focus, or <c>null</c> to skip focusing.</param>
    /// <param name="text">The error message text.</param>
    /// <param name="title">The message box title.</param>
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

    /// <summary>
    /// Displays a warning message box with OK/Cancel buttons and optionally focuses the specified control.
    /// </summary>
    /// <param name="control">The control to focus, or <c>null</c> to skip focusing.</param>
    /// <param name="text">The warning message text.</param>
    /// <param name="title">The message box title.</param>
    /// <returns><c>true</c> if the user clicked OK; otherwise, <c>false</c>.</returns>
    public static bool ShowWarning(Control? control, string text, string title = "Warning")
    {
        if (control != null)
        {
            SetFocus(control);
        }

        return !string.IsNullOrEmpty(text)
            && MessageBox.Show(text, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
    }

    /// <summary>
    /// Displays an informational message box and optionally focuses the specified control.
    /// </summary>
    /// <param name="control">The control to focus, or <c>null</c> to skip focusing.</param>
    /// <param name="text">The informational message text.</param>
    /// <param name="title">The message box title.</param>
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

    /// <summary>
    /// Sets focus to the specified control, navigating through parent TabControls as needed.
    /// </summary>
    /// <param name="control">The control to focus.</param>
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
