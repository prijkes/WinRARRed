namespace WinRARRed.Cryptography
{
    public class Hashing
    {
        private static readonly uint[] _lookup32UpperCase = CreateLookup32(true);

        private static readonly uint[] _lookup32LowerCase = CreateLookup32(false);

        private static uint[] CreateLookup32(bool upperCase)
        {
            uint[] result = new uint[256];

            for (int i = 0; i < 256; i++)
            {
                string s = upperCase ? i.ToString("X2") : i.ToString("x2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }

            return result;
        }

        public static string ByteArrayToHexViaLookup32(byte[] bytes, bool upperCase)
        {
            uint[] lookup32 = upperCase ? _lookup32UpperCase : _lookup32LowerCase;
            char[] result = new char[bytes.Length * 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                uint val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }

            return new string(result);
        }
    }
}
