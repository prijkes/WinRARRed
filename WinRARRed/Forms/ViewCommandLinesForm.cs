using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinRARRed.Diagnostics;

namespace WinRARRed.Forms
{
    public partial class ViewCommandLinesForm : Form
    {
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
                                (!a.ArchiveVersion.HasValue || a.ArchiveVersion.Value.HasFlag(archiveVersion))
                            ).Select(a => a.Argument);

                            string joinedArguments = string.Join(" ", filteredArguments);
                            string fileAttributeA = options.RAROptions.SetFileArchiveAttribute switch
                            {
                                CheckState.Checked => (a == 0 ? "attrib +A /S * && " : "attrib -A /S * && "),
                                CheckState.Indeterminate => "attrib +A /S * && ",
                                _ => string.Empty
                            };
                            string fileAttributeI = options.RAROptions.SetFileArchiveAttribute switch
                            {
                                CheckState.Checked => (a == 0 ? "attrib +I /S * && " : "attrib -I /S * && "),
                                CheckState.Indeterminate => "attrib +I /S * && ",
                                _ => string.Empty
                            };
                            arguments.Add($"{fileAttributeA}{fileAttributeI}{rarExeFilePath} {joinedArguments} {options.ReleaseDirectoryPath} .\\*");
                        });
                    });
                }
            }

            StringBuilder sb = new();
            foreach (string args in arguments.OrderBy(t => t))
            {
                sb.AppendLine(args);
            }
            tbCommandLines.Text = sb.ToString();
        }
    }
}
