using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WinRARRed.Diagnostics;

namespace WinRARRed.Forms
{
    public partial class SettingsOptionsForm : Form
    {
        public RAROptions RAROptions { get; private set; }

        public SettingsOptionsForm()
        {
            InitializeComponent();

            nupSwitchMTEnd.Value = Environment.ProcessorCount;

            cbFileA.CheckedChanged += CbFile_CheckedChanged;
            cbFileI.CheckedChanged += CbFile_CheckedChanged;
            cbSwitchAI.CheckedChanged += CbSwitchAI_CheckedChanged;
            cbSwitchMT.CheckedChanged += CbSwitchMT_CheckedChanged;
            cbSwitchV.CheckedChanged += CbSwitchV_CheckedChanged;
            btnCancel.Click += BtnCancel_Click;
            btnSave.Click += BtnSave_Click;

            RAROptions = GetRAROptions();

            Shown += OptionsForm_Shown;
        }

        public void Toggle(bool enabled)
        {
            gbRARVersion.Enabled = enabled;
            gbFileOptions.Enabled = enabled;
            gbSwitches.Enabled = enabled;
            cbDeleteRARFiles.Enabled = enabled;
            btnSave.Enabled = enabled;
        }

        private void OptionsForm_Shown(object? sender, EventArgs e)
        {
            cbRARVersion2.Checked = RAROptions.RARVersions.Any(r => r.Start == 200);
            cbRARVersion3.Checked = RAROptions.RARVersions.Any(r => r.Start == 300);
            cbRARVersion4.Checked = RAROptions.RARVersions.Any(r => r.Start == 400);
            cbRARVersion5.Checked = RAROptions.RARVersions.Any(r => r.Start == 500);
            cbRARVersion6.Checked = RAROptions.RARVersions.Any(r => r.Start == 600);
            cbFileA.CheckState = RAROptions.SetFileArchiveAttribute;
            cbFileI.CheckState = RAROptions.SetFileNotContentIndexedAttribute;
            cbSwitchAI.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-ai"));
            cbSwitchM0.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m0"));
            cbSwitchM1.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m1"));
            cbSwitchM2.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m2"));
            cbSwitchM3.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m3"));
            cbSwitchM4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m4"));
            cbSwitchM5.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m5"));
            cbSwitchMA4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-ma4"));
            cbSwitchMA5.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-ma5"));
            cbSwitchMD64K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md64")));
            cbSwitchMD128K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md128")));
            cbSwitchMD256K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md256")));
            cbSwitchMD512K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md512")));
            cbSwitchMD1024K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md1024")));
            cbSwitchMD2048K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md2048")));
            cbSwitchMD4096K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md4096")));
            cbSwitchMD8M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md8m"));
            cbSwitchMD16M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md16m"));
            cbSwitchMD32M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md32m"));
            cbSwitchMD64M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md64m"));
            cbSwitchMD128M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md128m"));
            cbSwitchMD256M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md256m"));
            cbSwitchMD512M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md512m"));
            cbSwitchMD1G.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md1g"));
            cbSwitchMT.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-mt")));
            var start = RAROptions.CommandLineArguments.Select(a => a.FirstOrDefault(ab => ab.Argument.StartsWith("-mt"))).FirstOrDefault();
            nupSwitchMTStart.Value = start != null ? int.Parse(start.Argument.Substring(3)) : 1;
            var end = RAROptions.CommandLineArguments.Select(a => a.LastOrDefault(ab => ab.Argument.StartsWith("-mt"))).LastOrDefault();
            nupSwitchMTEnd.Value = end != null ? int.Parse(end.Argument.Substring(3)) : Environment.ProcessorCount;
            cbSwitchR.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-r"));
            var volumes = RAROptions.CommandLineArguments.Select(a => a.FirstOrDefault(ab => ab.Argument.StartsWith("-v"))).FirstOrDefault();
            cbSwitchV.Checked = volumes != null;
            nupOptionsV.Value = volumes != null ? int.Parse(volumes.Argument.Substring(2, volumes.Argument.Length - 2 - (char.IsLetter(volumes.Argument.Last()) ? 1 : 0))) : 2;
            rbOptionsV1000.Checked = volumes != null && !char.IsLetter(volumes.Argument.Last());
            rbOptionsV1024.Checked = volumes== null || volumes != null && volumes.Argument.Last() == 'k';
            rbOptionsV1.Checked = volumes != null && volumes.Argument.Last() == 'b';
            cbSwitchTSM0.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm0"));
            cbSwitchTSM1.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm1"));
            cbSwitchTSM2.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm2"));
            cbSwitchTSM3.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm3"));
            cbSwitchTSM4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm4"));
            cbSwitchTSC0.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc0"));
            cbSwitchTSC1.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc1"));
            cbSwitchTSC2.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc2"));
            cbSwitchTSC3.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc3"));
            cbSwitchTSC4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc4"));
            cbSwitchTSA0.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa0"));
            cbSwitchTSA1.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa1"));
            cbSwitchTSA2.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa2"));
            cbSwitchTSA3.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa3"));
            cbSwitchTSA4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa4"));
            cbDeleteRARFiles.Checked = RAROptions.DeleteRARFiles;
        }

        private void CbFile_CheckedChanged(object? sender, EventArgs e)
        {
            cbSwitchAI.Enabled = !cbFileA.Checked && !cbFileI.Checked;
        }

        private void CbSwitchAI_CheckedChanged(object? sender, EventArgs e)
        {
            cbFileA.Enabled = cbFileI.Enabled = !cbSwitchAI.Checked;
        }

        private void CbSwitchMT_CheckedChanged(object? sender, EventArgs e)
        {
            nupSwitchMTStart.Enabled = nupSwitchMTEnd.Enabled = cbSwitchMT.Checked;
        }

        private void CbSwitchV_CheckedChanged(object? sender, EventArgs e)
        {
            nupOptionsV.Enabled = rbOptionsV1000.Enabled = rbOptionsV1024.Enabled = rbOptionsV1.Enabled = cbSwitchV.Checked;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            RAROptions options = GetRAROptions();

            if (!options.RARVersions.Any())
            {
                GUIHelper.ShowError(this, "No RAR version(s) have been selected.");
                return;
            }

            RAROptions = options;

            DialogResult = DialogResult.OK;
        }

        private void BtnCancel_Click(object? sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private RAROptions GetRAROptions()
        {
            List<VersionRange> rarVersions = [];
            if (cbRARVersion2.Checked)
            {
                rarVersions.Add(new(200, 300));
            }

            if (cbRARVersion3.Checked)
            {
                rarVersions.Add(new(300, 400));
            }

            if (cbRARVersion4.Checked)
            {
                rarVersions.Add(new(400, 500));
            }

            if (cbRARVersion5.Checked)
            {
                rarVersions.Add(new(500, 600));
            }

            if (cbRARVersion6.Checked)
            {
                rarVersions.Add(new(600, 700));
            }

            return new()
            {
                SetFileArchiveAttribute = cbFileA.CheckState,
                SetFileNotContentIndexedAttribute = cbFileI.CheckState,
                CommandLineArguments = CreateCommandLineArgumentsCombinations(),
                RARVersions = rarVersions,
                DeleteRARFiles = cbDeleteRARFiles.Checked
            };
        }

        private List<RARCommandLineArgument[]> CreateCommandLineArgumentsCombinations()
        {
            List<RARCommandLineArgument> compressionLevels = [];
            AddSwitch(compressionLevels, cbSwitchM0, new("-m0", 200));
            AddSwitch(compressionLevels, cbSwitchM1, new("-m1", 200));
            AddSwitch(compressionLevels, cbSwitchM2, new("-m2", 200));
            AddSwitch(compressionLevels, cbSwitchM3, new("-m3", 200));
            AddSwitch(compressionLevels, cbSwitchM4, new("-m4", 200));
            AddSwitch(compressionLevels, cbSwitchM5, new("-m5", 200));

            List<RARCommandLineArgument> archivingFormats = [];
            AddSwitch(archivingFormats, cbSwitchMA4, new("-ma4", 500));
            AddSwitch(archivingFormats, cbSwitchMA5, new("-ma5", 500));

            // RAR version 1,2,3 don't use 'k', 'm' or 'g' modifiers
            // RAR version 4,5,6 and later use 'm' by default if not specified; so we need to specify it
            List<RARCommandLineArgument> dictionarySizes = [];
            AddSwitch(dictionarySizes, cbSwitchMD64K, new("-md64", 200, 499, RARArchiveVersion.RAR4));
            AddSwitch(dictionarySizes, cbSwitchMD64K, new("-md64k", 200, 499, RARArchiveVersion.RAR4));
            AddSwitch(dictionarySizes, cbSwitchMD128K, new("-md128", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD128K, new("-md128k", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD256K, new("-md256", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD256K, new("-md256k", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD512K, new("-md512", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD512K, new("-md512k", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD1024K, new("-md1024", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD1024K, new("-md1024k", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD2048K, new("-md2048", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD2048K, new("-md2048k", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD4096K, new("-md4096", 200, 499, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD4096K, new("-md4096k", 500, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD8M, new("-md8m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD16M, new("-md16m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD32M, new("-md32m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD64M, new("-md64m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD128M, new("-md128m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD256M, new("-md256m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD512M, new("-md512m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD1G, new("-md1g", 500, RARArchiveVersion.RAR5));

            List<RARCommandLineArgument> modificationTimes = [];
            AddSwitch(modificationTimes, cbSwitchTSM0, new("-tsm0", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(modificationTimes, cbSwitchTSM1, new("-tsm1", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(modificationTimes, cbSwitchTSM2, new("-tsm2", 320, RARArchiveVersion.RAR4));
            AddSwitch(modificationTimes, cbSwitchTSM3, new("-tsm3", 320, RARArchiveVersion.RAR4));
            AddSwitch(modificationTimes, cbSwitchTSM4, new("-tsm4", 320, RARArchiveVersion.RAR4));

            List<RARCommandLineArgument> creationTimes = [];
            AddSwitch(creationTimes, cbSwitchTSC0, new("-tsc0", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(creationTimes, cbSwitchTSC1, new("-tsc1", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(creationTimes, cbSwitchTSC2, new("-tsc2", 320, RARArchiveVersion.RAR4));
            AddSwitch(creationTimes, cbSwitchTSC3, new("-tsc3", 320, RARArchiveVersion.RAR4));
            AddSwitch(creationTimes, cbSwitchTSC4, new("-tsc4", 320, RARArchiveVersion.RAR4));

            List<RARCommandLineArgument> accessTimes = [];
            AddSwitch(accessTimes, cbSwitchTSA0, new("-tsa0", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(accessTimes, cbSwitchTSA1, new("-tsa1", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(accessTimes, cbSwitchTSA2, new("-tsa2", 320, RARArchiveVersion.RAR4));
            AddSwitch(accessTimes, cbSwitchTSA3, new("-tsa3", 320, RARArchiveVersion.RAR4));
            AddSwitch(accessTimes, cbSwitchTSA4, new("-tsa4", 320, RARArchiveVersion.RAR4));

            List<RARCommandLineArgument[]> switchesList = [];
            for (int a = 0; a < Math.Max(compressionLevels.Count, 1); a++)
            {
                RARCommandLineArgument? compressionLevel = compressionLevels.Any() ? compressionLevels[a] : null;

                for (int b = 0; b < Math.Max(archivingFormats.Count, 1); b++)
                {
                    RARCommandLineArgument? archivingFormat = archivingFormats.Any() ? archivingFormats[b] : null;

                    for (int c = 0; c < Math.Max(dictionarySizes.Count, 1); c++)
                    {
                        RARCommandLineArgument? dictionarySize = dictionarySizes.Any() ? dictionarySizes[c] : null;

                        for (int d = 0; d < Math.Max(modificationTimes.Count, 1); d++)
                        {
                            RARCommandLineArgument? modificationTime = modificationTimes.Any() ? modificationTimes[d] : null;

                            for (int e = 0; e < Math.Max(creationTimes.Count, 1); e++)
                            {
                                RARCommandLineArgument? creationTime = creationTimes.Any() ? creationTimes[e] : null;

                                for (int f = 0; f < Math.Max(accessTimes.Count, 1); f++)
                                {
                                    RARCommandLineArgument? accessTime = accessTimes.Any() ? accessTimes[f] : null;

                                    for (int x = 0; x < (cbSwitchAI.Checked ? 2 : 1); x++)
                                    {
                                        for (int z = cbSwitchMT.Checked ? (int)nupSwitchMTStart.Value - 1 : 0; z < (cbSwitchMT.Checked ? (int)nupSwitchMTEnd.Value + 1 : 1); z++)
                                        {
                                            List<RARCommandLineArgument> switches =
                                                [
                                                    // a Add files to archive
                                                    new("a", 200)
                                                ];

                                            if (x == 0 && cbSwitchAI.Checked)
                                            {
                                                // ai - Ignore file attributes
                                                switches.Add(new("-ai", 200));
                                            }

                                            if (cbSwitchR.Checked)
                                            {
                                                // -r Recurse subdirectories
                                                switches.Add(new("-r", 200));
                                            }

                                            if (cbSwitchDS.Checked)
                                            {
                                                // -ds Disable name sort for solid archive
                                                switches.Add(new("-ds", 200));
                                            }

                                            if (cbSwitchSDash.Checked)
                                            {
                                                // -s- Disable solid archiving
                                                switches.Add(new("-s-", 200));
                                            }

                                            if (compressionLevel != null)
                                            {
                                                // -m<0..5> Set compression level (0-store...3-default...5-maximal)
                                                switches.Add(compressionLevel);
                                            }

                                            if (archivingFormat != null)
                                            {
                                                // -ma[4|5] Specify a version of archiving format
                                                switches.Add(archivingFormat);
                                            }

                                            if (dictionarySize != null)
                                            {
                                                // -md<n>[k,m,g] Dictionary size in KB, MB or GB
                                                switches.Add(dictionarySize);
                                            }

                                            if (modificationTime != null)
                                            {
                                                // -tsm[N]
                                                switches.Add(modificationTime);
                                            }

                                            if (creationTime != null)
                                            {
                                                // -tsa[N]
                                                switches.Add(creationTime);
                                            }

                                            if (accessTime != null)
                                            {
                                                // -tsa[N]
                                                switches.Add(accessTime);
                                            }

                                            if (cbSwitchV.Checked)
                                            {
                                                // -v<size>[k,b] Create volumes with size=<size>*1000 [*1024, *1]
                                                switches.Add(new(
                                                    $"-v{nupOptionsV.Value}{(rbOptionsV1024.Checked ? "k" : rbOptionsV1.Checked ? "b" : string.Empty)}", 200));
                                            }

                                            // -mt<threads> Set the number of threads
                                            if (z >= nupSwitchMTStart.Value && cbSwitchMT.Checked)
                                            {
                                                switches.Add(new($"-mt{z}", 360));
                                            }

                                            switchesList.Add([.. switches]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return switchesList;
        }

        private static void AddSwitch(List<RARCommandLineArgument> list, CheckBox checkbox, RARCommandLineArgument argument)
        {
            if (checkbox.Checked)
            {
                list.Add(argument);
            }
        }

    }
}
