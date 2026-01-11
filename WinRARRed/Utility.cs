namespace WinRARRed
{
    public static class Utility
    {
        private static readonly string[] SizeUnits = ["b", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB"];

        public static string GetUserFriendlySizeString(long bytes)
        {
            int index = 0;
            double value = bytes;
            while (value > 1024)
            {
                value /= 1024;
                index++;
            }

            return $"{value} {SizeUnits[index]}";
        }
    }
}
