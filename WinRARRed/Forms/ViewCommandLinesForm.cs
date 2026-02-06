using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinRARRed.Diagnostics;

namespace WinRARRed.Forms;

public partial class ViewCommandLinesForm : Form
{
    private string[] _allCommandLines = [];

    public ViewCommandLinesForm(BruteForceOptions options)
    {
        InitializeComponent();

        LoadCommandLines(options);

        Focus();
    }

    private void LoadCommandLines(BruteForceOptions options)
    {
        ConcurrentBag<string> arguments = [];
        string[] rarVersionDirectories = Directory.GetDirectories(options.RARInstallationsDirectoryPath);

        for (int a = 0; a < (options.RAROptions.SetFileArchiveAttribute == CheckState.Checked ? 2 : 1); a++)
        {
            for (int b = 0; b < (options.RAROptions.SetFileNotContentIndexedAttribute == CheckState.Checked ? 2 : 1); b++)
            {
                Parallel.ForEach(rarVersionDirectories, (rarVersionDirectoryPath, s, i) =>
                {
                    string rarExeFilePath = Path.Combine(rarVersionDirectoryPath, "rar.exe");
                    if (!File.Exists(rarExeFilePath))
                    {
                        return;
                    }

                    string rarVersionDirectoryName = Path.GetFileName(rarVersionDirectoryPath);
                    int version = Manager.ParseRARVersion(rarVersionDirectoryName);
                    if (!options.RAROptions.RARVersions.Any(r => r.InRange(version)))
                    {
                        return;
                    }

                    Parallel.ForEach(options.RAROptions.CommandLineArguments, (commandLineArguments, s2, j) =>
                    {
                        RARArchiveVersion archiveVersion = Manager.ParseRARArchiveVersion(commandLineArguments, version);

                    // Filter arguments by RAR version and RAR archive version
                    IEnumerable<string> filteredArguments = commandLineArguments.Where(
                            a => version >= a.MinimumVersion &&
                            (!a.MaximumVersion.HasValue || version <= a.MaximumVersion.Value) &&
                            (!a.ArchiveVersion.HasValue || a.ArchiveVersion.Value.HasFlag(archiveVersion))
                        ).Select(a => a.Argument);

                        string joinedArguments = string.Join(" ", filteredArguments);
                        string fileAttributeA = options.RAROptions.SetFileArchiveAttribute switch
                        {
                            CheckState.Checked => (a == 0 ? "attrib +A /S * && " : "attrib -A /S * && "),
                            CheckState.Indeterminate => "attrib +A /S * && ",
                            _ => string.Empty
                        };
                        string fileAttributeI = options.RAROptions.SetFileNotContentIndexedAttribute switch
                        {
                            CheckState.Checked => (b == 0 ? "attrib +I /S * && " : "attrib -I /S * && "),
                            CheckState.Indeterminate => "attrib +I /S * && ",
                            _ => string.Empty
                        };
                        arguments.Add($"{fileAttributeA}{fileAttributeI}{rarExeFilePath} {joinedArguments} {options.ReleaseDirectoryPath} .\\*");
                    });
                });
            }
        }

        _allCommandLines = arguments.OrderBy(t => t).ToArray();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string filterText = txtSearch.Text.Trim();
        listViewCommands.BeginUpdate();
        listViewCommands.Items.Clear();

        IEnumerable<string> filtered = string.IsNullOrEmpty(filterText)
            ? _allCommandLines
            : _allCommandLines.Where(line => line.Contains(filterText, StringComparison.OrdinalIgnoreCase));

        int index = 0;
        foreach (string line in filtered)
        {
            var item = new ListViewItem((++index).ToString());
            item.SubItems.Add(line);
            listViewCommands.Items.Add(item);
        }

        listViewCommands.EndUpdate();

        lblLineCount.Text = string.IsNullOrEmpty(filterText)
            ? $"{_allCommandLines.Length} command lines"
            : $"{index} of {_allCommandLines.Length} command lines";
    }

    private void TxtSearch_TextChanged(object? sender, EventArgs e)
    {
        ApplyFilter();
    }

    private void BtnCopySelected_Click(object? sender, EventArgs e)
    {
        if (listViewCommands.SelectedItems.Count == 0)
            return;

        var lines = new List<string>();
        foreach (ListViewItem item in listViewCommands.SelectedItems)
        {
            lines.Add(item.SubItems[1].Text);
        }
        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }

    private void BtnCopyAll_Click(object? sender, EventArgs e)
    {
        var lines = new List<string>();
        foreach (ListViewItem item in listViewCommands.Items)
        {
            lines.Add(item.SubItems[1].Text);
        }
        if (lines.Count > 0)
        {
            Clipboard.SetText(string.Join(Environment.NewLine, lines));
        }
    }
}
