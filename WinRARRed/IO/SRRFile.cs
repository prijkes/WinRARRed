using System.IO;

namespace WinRARRed.IO
{
    public class SRRFile
    {
        // SRR block types
        private readonly int BlockMark = 0x69;
        private readonly int StoredFile = 0x6A;
        private readonly int OsoHash = 0x6b;
        private readonly int RarFile = 0x71;

        // Flags for SRR marker block
        private readonly int AppNamePresent = 0x0001;


        public static SRRFile? Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            return null;
        }
    }
}
