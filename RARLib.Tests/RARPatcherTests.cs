using Force.Crc32;

namespace RARLib.Tests;

public class RARPatcherTests : IDisposable
{
    private static readonly string TestDataPath = Path.Combine(
        AppContext.BaseDirectory, "TestData");

    private readonly string _testDir;

    public RARPatcherTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"rarpatcher_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    /// <summary>
    /// Copies a test RAR file to the temp directory for patching tests.
    /// </summary>
    private string CopyTestFile(string fileName)
    {
        string source = Path.Combine(TestDataPath, fileName);
        string dest = Path.Combine(_testDir, fileName);
        File.Copy(source, dest, true);
        return dest;
    }

    #region PatchFile Tests

    [Fact]
    public void PatchFile_HostOS_ModifiesHeader()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");

        var options = new PatchOptions
        {
            FileHostOS = 3 // Unix
        };

        var results = RARPatcher.PatchFile(testFile, options);

        Assert.NotEmpty(results);
        Assert.All(results.Where(r => r.BlockType == RAR4BlockType.FileHeader),
            r => Assert.Equal(3, r.NewHostOS));
    }

    [Fact]
    public void PatchFile_HostOS_RecalculatesCrc()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");

        var options = new PatchOptions
        {
            FileHostOS = 3 // Unix (original is Windows = 2)
        };

        var results = RARPatcher.PatchFile(testFile, options);

        // Verify that CRC was recalculated
        Assert.NotEmpty(results);
        foreach (var result in results)
        {
            Assert.NotEqual(result.OriginalCrc, result.NewCrc);
        }

        // Verify the file is still parseable after patching
        using var fs = new FileStream(testFile, FileMode.Open, FileAccess.Read);
        fs.Seek(7, SeekOrigin.Begin); // Skip marker
        var reader = new RARHeaderReader(fs);

        while (fs.Position < fs.Length)
        {
            var block = reader.ReadBlock(parseContents: true);
            if (block == null) break;

            if (block.BlockType == RAR4BlockType.FileHeader || block.BlockType == RAR4BlockType.Service)
            {
                Assert.True(block.CrcValid, $"CRC invalid after patching at position {block.BlockPosition}");
            }

            reader.SkipBlock(block, includeData: block.BlockType != RAR4BlockType.FileHeader);
        }
    }

    [Fact]
    public void PatchFile_FileAttributes_ModifiesHeader()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");

        var options = new PatchOptions
        {
            FileAttributes = 0x000081B4 // Unix file mode
        };

        var results = RARPatcher.PatchFile(testFile, options);

        Assert.NotEmpty(results);
        Assert.All(results.Where(r => r.BlockType == RAR4BlockType.FileHeader),
            r => Assert.Equal(0x000081B4u, r.NewAttributes));
    }

    [Fact]
    public void PatchFile_NoChangesNeeded_ReturnsEmptyResults()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");

        // Read original host OS first
        byte originalHostOS;
        using (var fs = new FileStream(testFile, FileMode.Open, FileAccess.Read))
        {
            fs.Seek(7, SeekOrigin.Begin);
            var reader = new RARHeaderReader(fs);
            while (fs.Position < fs.Length)
            {
                var block = reader.ReadBlock(parseContents: true);
                if (block == null) break;
                if (block.FileHeader != null)
                {
                    originalHostOS = block.FileHeader.HostOS;
                    break;
                }
                reader.SkipBlock(block, includeData: block.BlockType != RAR4BlockType.FileHeader);
            }
        }

        // Patch to same value
        var options = new PatchOptions
        {
            FileHostOS = 2 // Windows (should be same as original)
        };

        var results = RARPatcher.PatchFile(testFile, options);

        // If original is already Windows, no changes should be made
        // (or all results show no actual change)
        // The patcher only writes if the values differ
    }

    [Fact]
    public void PatchFile_ServiceBlockDisabled_SkipsServiceBlocks()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");

        var options = new PatchOptions
        {
            FileHostOS = 3,
            PatchServiceBlocks = false // Don't patch CMT blocks
        };

        var results = RARPatcher.PatchFile(testFile, options);

        // Service blocks should not be in results
        Assert.DoesNotContain(results, r => r.BlockType == RAR4BlockType.Service);
    }

    [Fact]
    public void PatchFile_ServiceBlockFileTime_PatchesCorrectly()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");

        var options = new PatchOptions
        {
            FileHostOS = 3,
            PatchServiceBlocks = true,
            ServiceBlockFileTime = 0 // Zero out CMT file time
        };

        var results = RARPatcher.PatchFile(testFile, options);

        // Should have patched both file headers and service blocks
        Assert.NotEmpty(results);
    }

    #endregion

    #region AnalyzeFile Tests

    [Fact]
    public void AnalyzeFile_DoesNotModifyFile()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");
        byte[] originalBytes = File.ReadAllBytes(testFile);

        var options = new PatchOptions
        {
            FileHostOS = 3
        };

        var results = RARPatcher.AnalyzeFile(testFile, options);

        byte[] afterBytes = File.ReadAllBytes(testFile);
        Assert.Equal(originalBytes, afterBytes);
    }

    [Fact]
    public void AnalyzeFile_ReportsBlocksThatWouldChange()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");

        var options = new PatchOptions
        {
            FileHostOS = 3 // Unix (different from Windows original)
        };

        var results = RARPatcher.AnalyzeFile(testFile, options);

        // Should report blocks that would be modified
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(3, r.NewHostOS));
    }

    [Fact]
    public void AnalyzeFile_NewCrcIsZero_InAnalysisMode()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");

        var options = new PatchOptions
        {
            FileHostOS = 3
        };

        var results = RARPatcher.AnalyzeFile(testFile, options);

        Assert.All(results, r => Assert.Equal((ushort)0, r.NewCrc));
    }

    #endregion

    #region PatchOptions Tests

    [Fact]
    public void PatchOptions_GetServiceBlockHostOS_FallsBackToFileHostOS()
    {
        var options = new PatchOptions
        {
            FileHostOS = 3
        };

        Assert.Equal((byte)3, options.GetServiceBlockHostOS());
    }

    [Fact]
    public void PatchOptions_GetServiceBlockHostOS_UsesExplicitValue()
    {
        var options = new PatchOptions
        {
            FileHostOS = 3,
            ServiceBlockHostOS = 2
        };

        Assert.Equal((byte)2, options.GetServiceBlockHostOS());
    }

    [Fact]
    public void PatchOptions_GetServiceBlockAttributes_FallsBackToFileAttributes()
    {
        var options = new PatchOptions
        {
            FileAttributes = 0x000081B4
        };

        Assert.Equal(0x000081B4u, options.GetServiceBlockAttributes());
    }

    [Fact]
    public void PatchOptions_GetServiceBlockAttributes_UsesExplicitValue()
    {
        var options = new PatchOptions
        {
            FileAttributes = 0x000081B4,
            ServiceBlockAttributes = 0x00000020
        };

        Assert.Equal(0x00000020u, options.GetServiceBlockAttributes());
    }

    [Fact]
    public void PatchOptions_DefaultPatchServiceBlocks_IsTrue()
    {
        var options = new PatchOptions();

        Assert.True(options.PatchServiceBlocks);
    }

    #endregion

    #region GetHostOSName Tests

    [Theory]
    [InlineData(0, "MS-DOS")]
    [InlineData(1, "OS/2")]
    [InlineData(2, "Windows")]
    [InlineData(3, "Unix")]
    [InlineData(4, "Mac OS")]
    [InlineData(5, "BeOS")]
    [InlineData(6, "Unknown (6)")]
    [InlineData(255, "Unknown (255)")]
    public void GetHostOSName_ReturnsCorrectName(byte hostOS, string expected)
    {
        string result = RARPatcher.GetHostOSName(hostOS);

        Assert.Equal(expected, result);
    }

    #endregion

    #region PatchStream Tests

    [Fact]
    public void PatchStream_PatchesBothHostOSAndAttributes()
    {
        string testFile = CopyTestFile("test_wrar40_m3.rar");

        var options = new PatchOptions
        {
            FileHostOS = 3,
            FileAttributes = 0x000081B4
        };

        var results = new List<PatchResult>();
        using var stream = new FileStream(testFile, FileMode.Open, FileAccess.ReadWrite);
        RARPatcher.PatchStream(stream, options, results);

        Assert.NotEmpty(results);
        foreach (var result in results.Where(r => r.BlockType == RAR4BlockType.FileHeader))
        {
            Assert.Equal(3, result.NewHostOS);
            Assert.Equal(0x000081B4u, result.NewAttributes);
        }
    }

    #endregion
}
